using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace TornBot.Bot.Infrastructure.BackgroundJobs;

public class DeleteExpiredTrackedAttacksJob(
    IDbContextFactory<TornbotContext> dbContextFactory,
    ILogger<DeleteExpiredTrackedAttacksJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var factions = await dbContext.Factions
            .Include(f => f.TrackedAttacks)
            .ToListAsync(context.CancellationToken);

        foreach (var faction in factions)
        {
            var now = DateTime.UtcNow;
            var totalRemoved = faction.TrackedAttacks.RemoveAll(a => now >= a.Timestamp.AddMinutes(5));
            logger.LogInformation("Removed {TotalRemoved} expired tracked attacks for faction {FactionFactionId}",
                totalRemoved, faction.FactionId);
        }

        await dbContext.SaveChangesAsync(context.CancellationToken);
    }
}