using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
            .Include(faction => faction.TrackedAttacks)
            .ToListAsync();

        foreach (var faction in factions)
        {
            var outgoingAttacks = await attackService.GetOutgoingAttacksByIdAsync(faction.FactionId);
            var retalTargetDict = faction.TrackedAttacks
                .GroupBy(a => a.TargetPlayerId)
                .ToImmutableDictionary(a => a.Key);

            var validOutgoingAttacks = outgoingAttacks
                .Where(a => a.Result == AttackResult.Hospitalized)
                .Where(a => retalTargetDict.Keys.Contains(a.Defender.Id))
                .ToImmutableList();

            var config = faction.ModuleConfigs.SingleOrDefault(c => c.Module == Module.Retal);
            if (config is null)
            {
                logger.LogWarning("No retal module config found for faction {factionId}", faction.FactionId);
                continue;
            }

            var retalModuleConfig = config.Config.Deserialize<RetalModuleConfig>();
            if (retalModuleConfig is null)
            {
                logger.LogWarning("Could not deserialize retal module config for faction {factionId}",
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
                logger.LogInformation("Notification channel or role not set for faction {factionId}",
                    faction.FactionId);
                continue;
            }

            foreach (var trackedAttack in faction.TrackedAttacks)
            {
                var claimedRetal = validOutgoingAttacks
                    .Where(a => a.Defender.Id == trackedAttack.TargetPlayerId)
                    .Where(a => a.Timestamp > trackedAttack.Timestamp)
                    .OrderBy(a => a.Timestamp)
                    .FirstOrDefault();

                if (claimedRetal is null)
                    continue;

                trackedAttack.State = RetalOpportunityState.Claimed;

                // Modify message
                var message =
                    await restClient.GetMessageAsync(retalModuleConfig.NotificationChannelId.Value,
                        trackedAttack.MessageId);

                var claimedEmbed = new EmbedProperties()
                {
                    Title = "Retaliation",
                    Description = ""
                };

                await restClient.ModifyMessageAsync(retalModuleConfig.NotificationChannelId.Value, message.Id,
                    messageProperties =>
                    {
                        if (messageProperties.Embeds is null) return;
                        foreach (var embed in messageProperties.Embeds)
                        {
                            var stringBuilder = new StringBuilder(embed.Description);
                            stringBuilder.AppendLine();
                            stringBuilder.AppendLine(
                                $"Claimed by [{claimedRetal.Attacker!.Username}]({ShortUrlHelper.GetProfileUrl(claimedRetal.Attacker.Id)})");
                            claimedEmbed.Description = stringBuilder.ToString();
                            messageProperties.Embeds = [claimedEmbed];
                        }
                    });

                await dbContext.SaveChangesAsync();
            }
        }
    }
}