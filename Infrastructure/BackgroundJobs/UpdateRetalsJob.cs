using System.Collections.Immutable;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Rest;
using TornBot.Bot.Domain.Models;
using TornBot.Bot.Features.Retaliation;
using TornBot.Bot.Features.Retaliation.Models;
using TornBot.Bot.Infrastructure.TornApi;
using TornBot.Bot.Infrastructure.TornApi.Models;
using TornBot.Bot.Shared;

namespace TornBot.Bot.Infrastructure.BackgroundJobs;

// TODO: make use of facade pattern to reduce dependencies
// TODO: maybe create extension methods for RestClient class for sending notifications
public class UpdateRetalsJob(
    BattleStatService battleStatService,
    TornApiClient tornClient,
    AttackService attackService,
    RestClient restClient,
    NotificationService notificationService,
    ModuleConfigRepository repository,
    IDbContextFactory<TornbotContext> contextFactory,
    ILogger<UpdateRetalsJob> logger
) : FactionJob<UpdateRetalsJob>(contextFactory, logger)
{
    protected override async Task<List<Faction>> LoadFactionsAsync(TornbotContext dbContext, CancellationToken ct)
    {
        return await dbContext.Factions
            .Include(f => f.TrackedAttacks)
            .ToListAsync(ct);
    }

    protected override async Task ProcessFactionAsync(Faction faction, CancellationToken ct)
    {
        const int expiryTimeMinutes = 5;
        var retalConfig = await repository.GetRetalModuleConfigByGuildId(faction.GuildId);
        var expiredAttacks = faction.TrackedAttacks
            .Where(a => DateTime.UtcNow - a.Timestamp > TimeSpan.FromMinutes(expiryTimeMinutes))
            .ToImmutableList();

        await ProcessExpiredRetals(faction, expiredAttacks, retalConfig, ct);

        var trackedAttacksIdHashSet = faction.TrackedAttacks.Select(a => a.AttackId).ToImmutableHashSet();
        var incomingAttacks = await attackService.GetIncomingAttacks(faction.GuildId);
        var validIncomingAttacks = incomingAttacks
            .Where(a => a.Result is
                AttackResult.Hospitalized or
                AttackResult.Arrested or
                AttackResult.Attacked or
                AttackResult.Mugged or
                AttackResult.Bounty or
                AttackResult.Looted
            )
            .Where(a => !trackedAttacksIdHashSet.Contains((ulong)a.Id))
            .Where(a => a.Attacker != null)
            .Where(a =>
            {
                var difference = DateTime.UtcNow - DateTimeOffset.FromUnixTimeSeconds(a.Ended).UtcDateTime;
                return difference < TimeSpan.FromMinutes(expiryTimeMinutes);
            }
            )
            .OrderBy(a => a.Ended)
            .ToImmutableList();

        await ProcessIncomingAttacks(faction, validIncomingAttacks, retalConfig, ct);

        var outgoingAttacks = await attackService.GetOutgoingAttacks(faction.GuildId);
        var retalTargetDict = faction.TrackedAttacks
            .GroupBy(a => a.TargetPlayerId)
            .ToImmutableDictionary(a => a.Key);

        var validOutgoingAttacks = outgoingAttacks
            .Where(a => a.Result == AttackResult.Hospitalized)
            .Where(a => retalTargetDict.Keys.Contains(a.Defender.Id))
            .OrderBy(a => a.Ended)
            .ToImmutableList();

        await ProcessOutgoingAttacks(faction, validOutgoingAttacks, retalTargetDict, retalConfig, ct);
    }

    private async Task ProcessExpiredRetals(Faction faction, ImmutableList<RetalOpportunity> expiredAttacks,
        RetalModuleConfig? retalConfig, CancellationToken ct)
    {
        var incomingAttacks = await attackService.GetIncomingAttacks(faction.GuildId);
        var incomingAttacksIdHashSet = incomingAttacks.Select(a => a.Id).ToImmutableHashSet();
        
        foreach (var expiredAttack in expiredAttacks.Where(a => !incomingAttacksIdHashSet.Contains((int)a.AttackId)))
        {
            var message = await restClient.GetMessageAsync(retalConfig!.NotificationChannelId!.Value,
                expiredAttack.MessageId, cancellationToken: ct);

            await restClient.ModifyMessageAsync(retalConfig.NotificationChannelId!.Value, expiredAttack.MessageId,
                messageProperties =>
                {
                    messageProperties.Embeds =
                    [
                        new EmbedProperties
                        {
                            Title = "Retal Expired",
                            Description = message.Embeds[0].Description,
                            Color = new Color(255, 0, 0),
                        }
                    ];
                    messageProperties.Components = [];
                }, cancellationToken: ct);
        }

        var expiredAttacksHashSet = expiredAttacks.Select(e => e.Id).ToHashSet();
        faction.TrackedAttacks.RemoveAll(a => expiredAttacksHashSet.Contains(a.Id));
    }

    private async Task ProcessOutgoingAttacks(Faction faction, ImmutableList<AttackFull> validOutgoingAttacks,
        ImmutableDictionary<long, IGrouping<long, RetalOpportunity>> retalTargetDict, RetalModuleConfig? retalConfig,
        CancellationToken ct)
    {
        foreach (var outgoingAttack in validOutgoingAttacks)
        {
            var retalOpportunities = retalTargetDict[outgoingAttack.Defender.Id];

            // update all messages
            if (outgoingAttack.Attacker is null)
            {
                Logger.LogError(
                    "Something went wrong in getting data for outgoing attacks for faction {FactionId}, attacker is null",
                    faction.Id);
                continue;
            }

            var attackerBasic = await tornClient.GetUserProfileById(outgoingAttack.Attacker.Id, ct);
            if (attackerBasic == null)
            {
                Logger.LogError("Unable to get player profile for id {playerId} for guild id {guildId}",
                    outgoingAttack.Attacker.Id, faction.GuildId);
                continue;
            }

            foreach (var opportunity in retalOpportunities)
            {
                var message = await restClient.GetMessageAsync(retalConfig!.NotificationChannelId!.Value,
                    opportunity.MessageId, cancellationToken: ct);
                await restClient.ModifyMessageAsync(retalConfig.NotificationChannelId!.Value, opportunity.MessageId,
                    messageProperties =>
                    {
                        messageProperties.Embeds =
                        [
                            new EmbedProperties
                            {
                                Title = $"Retal claimed by {attackerBasic.Name}[{attackerBasic.Id}]",
                                Description = message.Embeds[0].Description,
                                Color = new Color(0, 255, 0)
                            }
                        ];
                        messageProperties.Components = [];
                    }, cancellationToken: ct);
            }
        }
    }

    private async Task ProcessIncomingAttacks(Faction faction, ImmutableList<AttackFull> validIncomingAttacks,
        RetalModuleConfig? retalConfig, CancellationToken ct)
    {
        foreach (var incomingAttack in validIncomingAttacks)
        {
            var attackerProfile = await tornClient.GetUserProfileById(incomingAttack.Attacker!.Id, ct);
            var defenderProfile = await tornClient.GetUserProfileById(incomingAttack.Defender.Id, ct);

            if (attackerProfile == null || defenderProfile == null)
            {
                Logger.LogError(
                    "Something went wrong requesting user info for: attacker {AttackerId}, defender {DefenderId}",
                    incomingAttack.Attacker.Id, incomingAttack.Defender.Id);
                continue;
            }

            // TODO: replace with enum values parsed from TORN API
            if (attackerProfile.Status.State is "Abroad" or "Traveling" or "Federal" or "Fallen")
            {
                Logger.LogInformation("Attacker is not available for retal status: {playerStatus}", attackerProfile.Status.State);
                continue;
            }

            var attackerBattleStats = await battleStatService.GetUserBattlestatsById(attackerProfile.Id);
            var retalMessage = await CreateRetalMessageAsync(incomingAttack.Result, attackerProfile, defenderProfile,
                attackerBattleStats);

            if (retalConfig == null)
            {
                Logger.LogWarning("Something went wrong while getting the retal config for guild id {GuildId}",
                    faction.GuildId);
                continue;
            }

            if (!retalConfig.NotificationChannelId.HasValue || !retalConfig.NotificationRoleId.HasValue)
            {
                Logger.LogInformation("Notification channel or role not set for guild {GuildId}", faction.GuildId);
                continue;
            }

            var message = await notificationService.SendNotificationAsync(retalConfig.NotificationChannelId.Value,
                retalMessage, retalConfig.NotificationRoleId);

            var newRetalOpportunity = new RetalOpportunity()
            {
                AttackId = (ulong)incomingAttack.Id,
                MessageId = message.Id,
                TargetPlayerId = incomingAttack.Attacker.Id
            };

            faction.TrackedAttacks.Add(newRetalOpportunity);
        }
    }

    private async Task<MessageProperties> CreateRetalMessageAsync(AttackResult result, Profile attacker,
        Profile defender, BattleStat? battleStat)
    {
        var stringBuilder = new StringBuilder();

        stringBuilder
            .AppendLine(
                $"[{attacker.Name}]({ShortUrlHelper.GetProfileUrl(attacker.Id)}) {result.ToString().ToLower()} [{defender.Name}]({ShortUrlHelper.GetProfileUrl(defender.Id)})");

        var faction = await tornClient.GetUserFactionAsync(attacker.Id);
        stringBuilder.AppendLine("### Player");
        stringBuilder.AppendLine($"[{attacker.Name}]({ShortUrlHelper.GetProfileUrl(attacker.Id)})");
        stringBuilder.AppendLine($"Level {attacker.Level}");

        if (faction is not null)
        {
            stringBuilder.AppendLine($"[{faction.Name}]({ShortUrlHelper.GetFactionUrl(faction.Id)})");
        }

        stringBuilder.AppendLine("### Battle stats");
        if (battleStat != null)
        {
            stringBuilder.AppendLine($"Total: {battleStat.TotalHumanReadable}");

            if (battleStat.Details is not null)
            {
                stringBuilder.AppendLine($"Strength {battleStat.Details.StrengthHumanReadable}");
                stringBuilder.AppendLine($"Defense {battleStat.Details.DefenseHumanReadable}");
                stringBuilder.AppendLine($"Speed {battleStat.Details.SpeedHumanReadable}");
                stringBuilder.AppendLine($"Dexterity {battleStat.Details.DexterityHumanReadable}");
            }
        }
        else
        {
            stringBuilder.AppendLine("** No battle stats found **");
        }

        return new MessageProperties
        {
            Embeds =
            [
                new EmbedProperties
                {
                    Title = "Retaliation opportunity",
                    Description = stringBuilder.ToString(),
                    Color = new Color(0, 0, 255)
                },
            ],
            Components =
            [
                new ActionRowProperties
                {
                    new LinkButtonProperties(ShortUrlHelper.GetAttackUrl(attacker.Id).ToString(), "Attack"),
                    new LinkButtonProperties(ShortUrlHelper.GetProfileUrl(attacker.Id).ToString(), "Profile")
                }
            ],
        };
    }
}