using System.Collections.Immutable;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetCord.Rest;
using TornBot.Bot.Domain.Enums;
using TornBot.Bot.Domain.Models;
using TornBot.Bot.Features.Retaliation.Models;
using TornBot.Bot.Infrastructure;
using TornBot.Bot.Infrastructure.BackgroundJobs;
using TornBot.Bot.Infrastructure.TornApi;
using TornBot.Bot.Infrastructure.TornApi.Models;
using TornBot.Bot.Shared;

namespace TornBot.Bot.Features.Retaliation;

public class GetIncomingAttacks(
    AttackService attackService,
    IDbContextFactory<TornbotContext> contextFactory,
    TornApiClient client,
    BattleStatService bsService,
    ModuleConfigRepository repository,
    RestClient restClient,
    ILogger<GetIncomingAttacks> logger
) : FactionJob<GetIncomingAttacks>(contextFactory, logger)
{
    protected override Task<List<Faction>> LoadFactionsAsync(TornbotContext dbContext, CancellationToken ct)
    {
        return dbContext.Factions
            .Include(f => f.TrackedAttacks)
            .ToListAsync(ct);
    }

    protected override async Task ProcessFactionAsync(Faction faction, CancellationToken ct)
    {
        var config = await repository.GetRetalModuleConfigByGuildId(faction.GuildId);
        if (ShouldSkip(config, faction.GuildId)) return;

        if (config!.State == ModuleState.Disabled)
        {
            Logger.LogWarning("Retal module is disabled for guild {GuildId}", faction.GuildId);
            return;
        }

        var trackedAttacks = faction.TrackedAttacks.Select(a => a.AttackId).ToImmutableHashSet();
        var attacks = await attackService.GetIncomingAttacks(faction.GuildId);

        foreach (var attack in attacks
                     .Where(a => !trackedAttacks.Contains((ulong)a.Id))
                     .Where(a => DateTime.UtcNow - a.Ended < TimeSpan.FromMinutes(5)))
        {
            if (attack.Attacker == null)
                continue;

            if (attack.Attacker.FactionId == attack.Defender.FactionId)
            {
                Logger.LogInformation("Attacker and defender from same faction with Id {FactionId}",
                    attack.Attacker.FactionId);
                continue;
            }

            var attackerBasic = await client.GetUserProfileById(attack.Attacker.Id, ct);
            var defenderBasic = await client.GetUserProfileById(attack.Defender.Id, ct);
            if (attackerBasic == null || defenderBasic == null)
            {
                Logger.LogWarning("Something went wrong requesting user info for: {AttackerId},{DefenderId}",
                    attack.Attacker.Id, attack.Defender.Id);
                continue;
            }

            if (!IsSuccessfulAttack(attack.Result)) continue;

            var playerStats = await bsService.GetUserBattlestatsById(attackerBasic.Id);
            var msg = await CreateRetalMessageAsync(attack.Result, attackerBasic, defenderBasic, playerStats);

            var message =
                await restClient.SendMessageAsync(config.NotificationChannelId!.Value, msg, cancellationToken: ct);
            var trackedAttack = new RetalOpportunity
            {
                AttackId = (ulong)attack.Id,
                MessageId = message.Id,
                TargetPlayerId = attack.Attacker.Id
            };

            faction.TrackedAttacks.Add(trackedAttack);
        }
    }

    private static bool IsSuccessfulAttack(AttackResult result) => result switch
    {
        AttackResult.Attacked or
            AttackResult.Mugged or
            AttackResult.Hospitalized or
            AttackResult.Arrested or
            AttackResult.Bounty => true,
        _ => false
    };

    private bool ShouldSkip(RetalModuleConfig? config, ulong guildId)
    {
        if (config == null)
        {
            Logger.LogWarning("No retal module config found for guild {GuildId}", guildId);
            return true;
        }

        if (!config.NotificationChannelId.HasValue)
        {
            Logger.LogWarning("No retal channel id set for retal module for guild {GuildId}", guildId);
            return true;
        }

        return false;
    }

    private async Task<MessageProperties> CreateRetalMessageAsync(AttackResult result, Profile attacker,
        Profile defender, BattleStat? battleStat)
    {
        var stringBuilder = new StringBuilder();

        stringBuilder
            .AppendLine(
                $"[{attacker.Name}]({ShortUrlHelper.GetProfileUrl(attacker.Id)}) {result.ToString().ToLower()} [{defender.Name}]({ShortUrlHelper.GetProfileUrl(defender.Id)})");

        var faction = await client.GetUserFactionAsync(attacker.Id);
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
                },
            ],
            Components =
            [
                new ActionRowProperties
                {
                    new LinkButtonProperties(ShortUrlHelper.GetAttackUrl(attacker.Id).ToString(), "Attack"),
                    new LinkButtonProperties(ShortUrlHelper.GetProfileUrl(attacker.Id).ToString(), "Profile")
                }
            ]
        };
    }
}