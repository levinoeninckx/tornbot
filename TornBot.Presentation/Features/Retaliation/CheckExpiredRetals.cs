using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Rest;
using Quartz;
using TornBot.Infrastructure.Persistence;

namespace TornBot.Bot.Features.Retaliation;

public class CheckExpiredRetals(
    IDbContextFactory<TornbotContext> contextFactory,
    ModuleConfigRepository repository,
    RestClient client,
    ILogger<CheckExpiredRetals> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var factions = await dbContext.Factions
            .Include(f => f.TrackedAttacks)
            .ToListAsync();


        foreach (var faction in factions)
        {
            var config = await repository.GetRetalModuleConfigByGuildId(faction.GuildId);
            var expiredAttacks = faction.TrackedAttacks.Where(a =>
                DateTime.UtcNow - a.Timestamp > TimeSpan.FromMinutes(5)).ToImmutableList();

            await Parallel.ForEachAsync(expiredAttacks, new ParallelOptions { MaxDegreeOfParallelism = 5 },
                async (expiredAttack, ct) =>
                {
                    var message = await client.GetMessageAsync(config!.NotificationChannelId!.Value,
                        expiredAttack.MessageId, cancellationToken: ct);
                    await client.ModifyMessageAsync(config!.NotificationChannelId!.Value, expiredAttack.MessageId,
                        messageProperties =>
                        {
                            messageProperties.Embeds =
                            [
                                new EmbedProperties
                                {
                                    Title = "Retal Expired",
                                    Description = message.Embeds[0].Description,
                                    Color = new Color(255, 0, 0),
                                }
                            ];
                            messageProperties.Components = [];
                        }, cancellationToken: ct);
                });

            var expiredAttacksHashSet = expiredAttacks.Select(e => e.Id).ToHashSet();
            faction.TrackedAttacks.RemoveAll(a => expiredAttacksHashSet.Contains(a.Id));
        }

        await dbContext.SaveChangesAsync();
    }
}