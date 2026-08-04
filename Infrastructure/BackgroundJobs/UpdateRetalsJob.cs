using System.Collections.Immutable;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Rest;
using TornBot.Bot.Domain.Enums;
using TornBot.Bot.Domain.Models;
using TornBot.Bot.Features.Retaliation;
using TornBot.Bot.Features.Retaliation.Models;
using TornBot.Bot.Infrastructure.TornApi;
using TornBot.Bot.Shared;
using BattleStat = TornBot.Bot.Domain.ValueObjects.BattleStat;

namespace TornBot.Bot.Infrastructure.BackgroundJobs;

// TODO: make use of facade pattern to reduce dependencies
// TODO: maybe create extension methods for RestClient class for sending notifications
public class UpdateRetalsJob(
    IPlayerProvider playerProvider,
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
            .Where(a => (DateTime.UtcNow - a.Timestamp.ToUniversalTime()) > TimeSpan.FromMinutes(expiryTimeMinutes))
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

        await ProcessOutgoingAttacks(faction, validOutgoingAttacks, retalConfig, ct);
    }

    private async Task ProcessExpiredRetals(Faction faction, ImmutableList<RetalOpportunity> expiredAttacks,
        RetalModuleConfig? retalConfig, CancellationToken ct)
    {
        foreach (var expiredAttack in expiredAttacks)
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

    private async Task ProcessOutgoingAttacks(
        Faction faction,
        ImmutableList<AttackFull> validOutgoingAttacks,
        RetalModuleConfig? retalConfig,
        CancellationToken ct
    )
    {
        var outgoingAttackDict = validOutgoingAttacks
            .GroupBy(a => a.Defender.Id)
            .Select(g => g.OrderBy(a => a.Ended).First())
            .ToImmutableDictionary(a => a.Defender.Id);
        foreach (var trackedAttack in faction.TrackedAttacks)
        {
            var outgoingAttack =
                CollectionExtensions.GetValueOrDefault(outgoingAttackDict, (int)trackedAttack.TargetPlayerId);
            if (outgoingAttack == null)
            {
                continue;
            }

            if (outgoingAttack.Attacker is null)
            {
                Logger.LogError(
                    "Something went wrong in getting data for outgoing attacks for faction {FactionId}, attacker is null",
                    faction.Id);
                continue;
            }

            trackedAttack.State = RetalOpportunityState.Claimed;

            var attackerBasic =
                await playerProvider.GetPlayerByTornIdAsync(outgoingAttack.Attacker.Id, faction.GuildId);

            if (attackerBasic == null)
            {
                Logger.LogError(
                    "Something went wrong in getting data for outgoing attacks for faction {FactionId}, attacker is null",
                    faction.Id);
                continue;
            }

            var message = await restClient.GetMessageAsync(retalConfig!.NotificationChannelId!.Value,
                trackedAttack.MessageId, cancellationToken: ct);
            await restClient.ModifyMessageAsync(retalConfig.NotificationChannelId!.Value, trackedAttack.MessageId,
                messageProperties =>
                {
                    messageProperties.Embeds =
                    [
                        new EmbedProperties
                        {
                            Title = $"Retal claimed by {attackerBasic.Username}[{attackerBasic.Id}]",
                            Description = message.Embeds[0].Description,
                            Color = new Color(0, 255, 0)
                        }
                    ];
                    messageProperties.Components = [];
                }, cancellationToken: ct
            );
        }
    }

    private async Task ProcessIncomingAttacks(Faction faction, ImmutableList<AttackFull> validIncomingAttacks,
        RetalModuleConfig? retalConfig, CancellationToken ct)
    {
        foreach (var incomingAttack in validIncomingAttacks)
        {
            var attackerProfile =
                await playerProvider.GetPlayerByTornIdAsync(incomingAttack.Attacker!.Id, faction.GuildId);
            var defenderProfile =
                await playerProvider.GetPlayerByTornIdAsync(incomingAttack.Defender.Id, faction.GuildId);

            if (attackerProfile == null || defenderProfile == null)
            {
                Logger.LogError(
                    "Something went wrong requesting user info for: attacker {AttackerId}, defender {DefenderId}",
                    incomingAttack.Attacker.Id, incomingAttack.Defender.Id);
                continue;
            }

            if (attackerProfile.FactionId == faction.FactionId)
            {
                Logger.LogInformation("Attacker {attackerId} is faction member, skipping", attackerProfile.Id);
                continue;
            }

            // TODO: replace with enum values parsed from TORN API
            if (attackerProfile.State is PlayerState.Abroad or PlayerState.Traveling or PlayerState.Federal
                or PlayerState.Fallen)
            {
                Logger.LogInformation("Attacker is not available for retal status: {playerStatus}",
                    attackerProfile.State);
                continue;
            }

            var retalMessage = await CreateRetalMessageAsync(incomingAttack.Result, attackerProfile, defenderProfile,
                attackerProfile.BattleStat);

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
                TargetPlayerId = incomingAttack.Attacker.Id,
                Timestamp = DateTimeOffset.FromUnixTimeSeconds(incomingAttack.Ended).UtcDateTime
            };

            faction.TrackedAttacks.Add(newRetalOpportunity);
        }
    }

    private async Task<MessageProperties> CreateRetalMessageAsync(AttackResult result, Player attacker,
        Player defender, BattleStat? battleStat)
    {
        var stringBuilder = new StringBuilder();

        stringBuilder
            .AppendLine(
                $"[{attacker.Username}]({ShortUrlHelper.GetProfileUrl(attacker.Id)}) {result.ToString().ToLower()} [{defender.Username}]({ShortUrlHelper.GetProfileUrl(defender.Id)})");

        var faction = await tornClient.GetUserFactionAsync(attacker.Id);
        stringBuilder.AppendLine("### Player");
        stringBuilder.AppendLine($"[{attacker.Username}]({ShortUrlHelper.GetProfileUrl(attacker.Id)})");
        stringBuilder.AppendLine($"Level {attacker.Level}");

        if (faction is not null)
        {
            stringBuilder.AppendLine($"[{faction.Name}]({ShortUrlHelper.GetFactionUrl(faction.Id)})");
        }

        stringBuilder.AppendLine("### Battle stats");
        if (battleStat != null)
        {
            stringBuilder.AppendLine($"Total: {battleStat.Estimate.ToHumanReadable()}");

            if (battleStat.Details is not null)
            {
                stringBuilder.AppendLine($"Strength {battleStat.Details.Strength.ToHumanReadable()}");
                stringBuilder.AppendLine($"Defense {battleStat.Details.Defense.ToHumanReadable()}");
                stringBuilder.AppendLine($"Speed {battleStat.Details.Speed.ToHumanReadable()}");
                stringBuilder.AppendLine($"Dexterity {battleStat.Details.Dexterity.ToHumanReadable()}");
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