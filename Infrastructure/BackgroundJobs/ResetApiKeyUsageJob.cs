using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;
using TornBot.Bot.Domain.Enums;

namespace TornBot.Bot.Infrastructure.BackgroundJobs;

public class ResetApiKeyUsageJob(
    IDbContextFactory<TornbotContext> dbContextFactory,
    ILogger<ResetApiKeyUsageJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            var apiKeys = await dbContext.ApiKeys.ToListAsync();

            foreach (var apiKey in apiKeys)
            {
                apiKey.UsageCount = 0;
            }

            var rowsAffected = await dbContext.SaveChangesAsync();
            logger.LogInformation("Reset {count} keys to 0 usage", rowsAffected);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error in {jobName}", nameof(ResetApiKeyUsageJob));
            throw new JobExecutionException() { RefireImmediately = true };
        }
    }
}
