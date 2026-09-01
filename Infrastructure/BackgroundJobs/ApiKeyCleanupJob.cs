using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;
using TornBot.Bot.Domain.Enums;
using TornBot.Bot.Domain.Models;
using TornBot.Bot.Infrastructure.FFScouter;
using TornBot.Bot.Infrastructure.TornApi;
using TornBot.Bot.Infrastructure.TornStats;

namespace TornBot.Bot.Infrastructure.BackgroundJobs;

public class ApiKeyCleanupJob(
    IDbContextFactory<TornbotContext> dbContextFactory,
    TornClient tornClient,
    FfScouterClient ffScouterClient,
    TornStatClient tornStatClient,
    ILogger<ApiKeyCleanupJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var ct = context.CancellationToken;

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        try
        {
            var apiKeys = await dbContext.ApiKeys.ToListAsync(ct);

            foreach (var apiKey in apiKeys)
            {
                if (ct.IsCancellationRequested)
                {
                    break;
                }

                var isValid = await IsKeyValidAsync(apiKey, ct);

                if (!isValid)
                {
                    dbContext.ApiKeys.Remove(apiKey);
                    logger.LogInformation("Deleted invalid API key: {Key}", apiKey.Key);
                }
            }

            await dbContext.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in {JobName}", nameof(ApiKeyCleanupJob));
            throw new JobExecutionException() { RefireImmediately = true };
        }
    }

    private async Task<bool> IsKeyValidAsync(ApiKey apiKey, CancellationToken ct)
    {
        try
        {
            return apiKey.AccessLevel switch
            {
                AccessLevel.FfScouter => await ffScouterClient.IsApiKeyValid(apiKey.Key),
                AccessLevel.TornStats => await tornStatClient.IsKeyValidAsync(apiKey.Key),
                AccessLevel.Public or AccessLevel.Minimal or AccessLevel.LimitedAccess or AccessLevel.Full =>
                    await tornClient.GetKeyInfoAsync(apiKey.Key, ct) is not null,
                _ => false
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while validating API key {Key} with access level {AccessLevel}",
                apiKey.Key, apiKey.AccessLevel);
            return false;
        }
    }
}