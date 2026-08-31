using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Rest;
using Quartz;
using TornBot.Bot.Domain.Enums;
using TornBot.Bot.Domain.Models;
using TornBot.Bot.Shared;
using AttackResult = TornBot.Bot.Domain.Enums.AttackResult;

namespace TornBot.Bot.Infrastructure.BackgroundJobs;

public class GetOutgoingAttacksJob(
    IDbContextFactory<TornbotContext> dbContextFactory,
    IAttackService attackService,
    IPlayerProvider playerProvider,
    ILogger<GetOutgoingAttacksJob> logger,
    RestClient restClient) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            var factions = await dbContext.Factions
                .AsSplitQuery()
                .Include(f => f.ModuleConfigs)
                .Include(f => f.TrackedAttacks.Where(a => a.State == RetalOpportunityState.Open))
                .Include(f => f.ApiKeys)
                .ToListAsync();

            foreach (var faction in factions)
            {
                var limitedKey = faction.GetKey(AccessLevel.LimitedAccess, requireFactionAccess: true);
                if (limitedKey is null)
                {
                    logger.LogInformation(
                        "Faction with id {factionId} does not have a limited key with faction api access",
                        faction.Id);
                    continue;
                }

                var retalTargetDict = faction.TrackedAttacks
                    .GroupBy(a => a.TargetPlayerId)
                    .ToImmutableDictionary(a => a.Key);

                if (GetRetalModuleConfig(faction, out var retalModuleConfig)) continue;

                var outgoingAttacks = await attackService.GetOutgoingAttacksByIdAsync(faction.FactionId, limitedKey);
                var validOutgoingAttacks = outgoingAttacks
                    .Where(a => a.Result == AttackResult.Hospitalized)
                    .Where(a => retalTargetDict.Keys.Contains(a.DefenderId))
                    .OrderBy(a => a.Timestamp)
                    .ToImmutableList();

                logger.LogInformation("Found {count} new retal opportunities for faction {factionId}",
                    validOutgoingAttacks.Count, faction.FactionId);

                foreach (var trackedAttack in faction.TrackedAttacks)
                {
                    var claimedRetal = validOutgoingAttacks
                        .Where(a => a.DefenderId == trackedAttack.TargetPlayerId)
                        .FirstOrDefault(a => a.Timestamp >= trackedAttack.Timestamp &&
                                             a.Timestamp <= trackedAttack.Timestamp.AddMinutes(5));

                    if (claimedRetal is null)
                        continue;

                    trackedAttack.State = RetalOpportunityState.Claimed;

                    try
                    {
                        var message =
                            await restClient.GetMessageAsync(retalModuleConfig!.NotificationChannelId!.Value,
                                trackedAttack.MessageId);

                        var ffKey = faction.GetKey(AccessLevel.FfScouter);
                        var tsKey = faction.GetKey(AccessLevel.TornStats);
                        var publicKey = faction.GetKey(AccessLevel.Public);

                        if (ffKey is null || tsKey is null || publicKey is null)
                        {
                            logger.LogError(
                                "Could not find ffScouter, Tornstats or publicKey api key for faction {factionId}",
                                faction.FactionId);
                            continue;
                        }

                        var ffScouterKey = new FFScouterApiKey(ffKey.Key, ffKey.TornPlayerId);
                        var tornStatsKey = new TornStatApiKey(ffKey.Key, ffKey.TornPlayerId);
                        var attacker = await playerProvider.GetPlayerByTornIdAsync(claimedRetal.AttackerId!.Value,
                            publicKey, ffScouterKey, tornStatsKey);
                        if (attacker is null)
                        {
                            logger.LogWarning("Could not find attacker {attackerId} for attack {attackId}",
                                claimedRetal.AttackerId, claimedRetal.Id);
                            continue;
                        }

                        await UpdateRetalMessageAsync(retalModuleConfig, message, attacker);
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "Failed to update retal message for faction {factionId}", faction.FactionId);
                        trackedAttack.State = RetalOpportunityState.Open;
                    }
                }
            }

            await dbContext.SaveChangesAsync();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error in {jobName}", nameof(GetOutgoingAttacksJob));

            if (context.RefireCount < 3)
                throw new JobExecutionException { RefireImmediately = true };
        }
    }

    private async Task UpdateRetalMessageAsync(RetalModuleConfig retalModuleConfig, RestMessage message,
        Player? attacker)
    {
        await restClient.ModifyMessageAsync(retalModuleConfig.NotificationChannelId!.Value, message.Id,
            messageProperties =>
            {
                if (messageProperties.Embeds is null) return;

                var messageEmbed = messageProperties.Embeds.First();
                var descriptionBuilder = new StringBuilder(messageEmbed.Description);
                descriptionBuilder.AppendLine();
                descriptionBuilder.AppendLine(
                    $"Claimed by [{attacker!.Username}]({ShortUrlHelper.GetProfileUrl(attacker.Id)})");

                messageProperties.Embeds =
                [
                    new EmbedProperties
                    {
                        Title = "Retaliation claimed",
                        Description = descriptionBuilder.ToString(),
                        Color = new Color(0, 255, 0)
                    }
                ];
            }
        );
    }

    private bool GetRetalModuleConfig(Faction faction, out RetalModuleConfig? retalModuleConfig)
    {
        var config = faction.ModuleConfigs.SingleOrDefault(c => c.Module == Module.Retal);
        if (config is null)
        {
            logger.LogWarning("No retal module config found for faction {factionId}", faction.FactionId);
            retalModuleConfig = null;
            return true;
        }

        retalModuleConfig = config.Config.Deserialize<RetalModuleConfig>();
        if (retalModuleConfig is null)
        {
            logger.LogError("Could not deserialize retal module config for faction {factionId}",
                faction.FactionId);
            return true;
        }

        if (retalModuleConfig.State != ModuleState.Enabled)
        {
            logger.LogInformation("Retal module is disabled for faction {factionId}", faction.FactionId);
            return true;
        }

        if (!retalModuleConfig.NotificationChannelId.HasValue || !retalModuleConfig.NotificationRoleId.HasValue)
        {
            logger.LogWarning("Notification channel or role not set for faction {factionId}",
                faction.FactionId);
            return true;
        }

        return false;
    }
}