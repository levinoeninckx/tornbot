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

        var now = DateTime.UtcNow;

        var totalRemoved = dbContext.TrackedAttacks
            .Where(a => a.Timestamp <= now.AddMinutes(-5))
            .ExecuteDeleteAsync();

        logger.LogInformation("Deleted {count} expired tracked attacks", totalRemoved);
    }
}