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
                if (apiKey.AccessLevel != AccessLevel.TornStats && apiKey.AccessLevel != AccessLevel.FfScouter)
                {
                    apiKey.UsageCount = 0;
                }
            }

            await dbContext.SaveChangesAsync();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error in {jobName}", nameof(ResetApiKeyUsageJob));
        }
    }
}
