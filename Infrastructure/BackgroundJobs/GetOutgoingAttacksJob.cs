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
    ILogger<GetOutgoingAttacksJob> logger,
    RestClient restClient) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var factions = await dbContext.Factions
            .Include(f => f.ModuleConfigs)
            .Include(f => f.TrackedAttacks)
            .Include(f => f.ApiKeys)
            .ToListAsync();

        foreach (var faction in factions)
        {
            var limitedKey = faction.GetKey(AccessLevel.LimitedAccess, requireFactionAccess: true);
            if (limitedKey is null)
            {
                logger.LogInformation("Faction with id {factionId} does not have a limited key with faction api access", faction.Id);
                continue;
            }

            var outgoingAttacks = await attackService.GetOutgoingAttacksByIdAsync(faction.FactionId, limitedKey);
            var retalTargetDict = faction.TrackedAttacks
                .GroupBy(a => a.TargetPlayerId)
                .ToImmutableDictionary(a => a.Key);

            var config = faction.ModuleConfigs.SingleOrDefault(c => c.Module == Module.Retal);
            if (config is null)
            {
                logger.LogWarning("No retal module config found for faction {factionId}", faction.FactionId);
                continue;
            }

            var retalModuleConfig = config.Config.Deserialize<RetalModuleConfig>();
            if (retalModuleConfig is null)
            {
                logger.LogError("Could not deserialize retal module config for faction {factionId}",
                    faction.FactionId);
                continue;
            }

            if (retalModuleConfig.State != ModuleState.Enabled)
            {
                logger.LogInformation("Retal module is disabled for faction {factionId}", faction.FactionId);
                continue;
            }

            if (!retalModuleConfig.NotificationChannelId.HasValue || !retalModuleConfig.NotificationRoleId.HasValue)
            {
                logger.LogWarning("Notification channel or role not set for faction {factionId}",
                    faction.FactionId);
                continue;
            }

            var validOutgoingAttacks = outgoingAttacks
                .Where(a => a.Result == AttackResult.Hospitalized)
                .Where(a => retalTargetDict.Keys.Contains(a.DefenderId))
                .ToImmutableList();

            foreach (var trackedAttack in faction.TrackedAttacks)
            {
                var claimedRetal = validOutgoingAttacks
                    .Where(a => a.DefenderId == trackedAttack.TargetPlayerId)
                    .Where(a => a.Timestamp > trackedAttack.Timestamp &&
                                a.Timestamp <= trackedAttack.Timestamp.AddMinutes(5))
                    .OrderBy(a => a.Timestamp)
                    .FirstOrDefault();

                if (claimedRetal is null)
                    continue;

                trackedAttack.State = RetalOpportunityState.Claimed;

                var message =
                    await restClient.GetMessageAsync(retalModuleConfig.NotificationChannelId.Value,
                        trackedAttack.MessageId);

                await restClient.ModifyMessageAsync(retalModuleConfig.NotificationChannelId.Value, message.Id,
                    messageProperties =>
                    {
                        if (messageProperties.Embeds is null) return;

                        var messageEmbed = messageProperties.Embeds.First();
                        var descriptionBuilder = new StringBuilder(messageEmbed.Description);
                        descriptionBuilder.AppendLine();
                        descriptionBuilder.AppendLine(
                            $"Claimed by [{claimedRetal.Attacker!.Username}]({ShortUrlHelper.GetProfileUrl(claimedRetal.Attacker.Id)})");

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

            var rowsAffected = await dbContext.SaveChangesAsync();
            logger.LogInformation("Updated {count} tracked attacks for faction {factionId}", rowsAffected, faction.FactionId);
        }
    }
}
