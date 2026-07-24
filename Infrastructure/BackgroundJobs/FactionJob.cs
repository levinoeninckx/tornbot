using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;
using TornBot.Bot.Domain.Models;

namespace TornBot.Bot.Infrastructure.BackgroundJobs;

public abstract class FactionJob<TJob>(
    IDbContextFactory<TornbotContext> contextFactory,
    ILogger<TJob> logger) : IJob
{
    protected ILogger<TJob> Logger { get; } = logger;

    protected abstract Task<List<Faction>> LoadFactionsAsync(TornbotContext dbContext, CancellationToken ct);

    protected abstract Task ProcessFactionAsync(Faction faction, CancellationToken ct);

    public async Task Execute(IJobExecutionContext context)
    {
        var ct = context.CancellationToken;
        try
        {
            await using var dbContext = await contextFactory.CreateDbContextAsync(ct);
            var factions = await LoadFactionsAsync(dbContext, ct);

            foreach (var faction in factions)
            {
                try
                {
                    await ProcessFactionAsync(faction, ct);
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Failed to process faction {FactionId} in {JobName}",
                        faction.FactionId, typeof(TJob).Name);
                }
            }

            await dbContext.SaveChangesAsync(ct);
        }
        catch (Exception e)
        {
            Logger.LogCritical(e, "Something went wrong while running the {JobName} job", typeof(TJob).Name);
        }
    }
}