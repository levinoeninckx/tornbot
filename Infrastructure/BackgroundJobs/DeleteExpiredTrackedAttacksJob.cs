using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Rest;
using Quartz;
using TornBot.Bot.Domain.Enums;
using TornBot.Bot.Domain.Models;

namespace TornBot.Bot.Infrastructure.BackgroundJobs;

public class DeleteExpiredTrackedAttacksJob(
    IDbContextFactory<TornbotContext> dbContextFactory,
    RestClient restClient,
    ILogger<DeleteExpiredTrackedAttacksJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var factions = await dbContext.Factions
            .Include(f => f.TrackedAttacks)
            .Include(f => f.ModuleConfigs)
            .ToListAsync();

        foreach (var faction in factions)
        {
            var expiredAttacks = faction.TrackedAttacks
                .Where(a => a.Timestamp <= DateTime.UtcNow.AddMinutes(-5))
                .Where(a => a.State == RetalOpportunityState.Open)
                .ToList();

            var claimedAttacks = faction.TrackedAttacks
                .Where(a => a.State == RetalOpportunityState.Claimed);

            dbContext.TrackedAttacks.RemoveRange(expiredAttacks);
            dbContext.TrackedAttacks.RemoveRange(claimedAttacks);

            var config = faction.ModuleConfigs
                .Single(m => m.Module == Module.Retal);

            var retalConfig = config.Config.Deserialize<RetalModuleConfig>();

            if (retalConfig is null)
            {
                logger.LogError("Could not deserialize retal module config for faction {factionId}",
                    faction.FactionId);
                continue;
            }

            if (retalConfig.State != ModuleState.Enabled)
                continue;

            var updateTasks =
                expiredAttacks.Select(a => UpdateMessageAsync(retalConfig.NotificationChannelId!.Value, a));

            await Task.WhenAll(updateTasks);
        }

        await dbContext.SaveChangesAsync();
    }

    private async Task UpdateMessageAsync(ulong channelId, RetalOpportunity opportunity)
    {
        var message = await restClient.GetMessageAsync(channelId, opportunity.MessageId);
        await restClient.ModifyMessageAsync(channelId, message.Id, messageProperties =>
        {
            messageProperties.Embeds =
            [
                new EmbedProperties
                {
                    Title = "Retaliation Expired",
                    Description = message.Embeds[0].Description,
                    Color = new Color(255, 0, 0)
                }
            ];
            messageProperties.Components = [];
        });
    }
}
