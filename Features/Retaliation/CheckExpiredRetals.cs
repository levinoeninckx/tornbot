using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Rest;
using TornBot.Bot.Domain.Models;
using TornBot.Bot.Infrastructure;
using TornBot.Bot.Infrastructure.BackgroundJobs;

namespace TornBot.Bot.Features.Retaliation;

public class CheckExpiredRetals(
    IDbContextFactory<TornbotContext> contextFactory,
    ModuleConfigRepository repository,
    RestClient client,
    ILogger<CheckExpiredRetals> logger
) : FactionJob<CheckExpiredRetals>(contextFactory, logger)
{
    protected override Task<List<Faction>> LoadFactionsAsync(TornbotContext dbContext, CancellationToken ct)
    {
        return dbContext.Factions
            .Include(f => f.TrackedAttacks)
            .ToListAsync(ct);
    }

    protected override async Task ProcessFactionAsync(Faction faction, CancellationToken ct)
    {
        var config = await repository.GetRetalModuleConfigByGuildId(faction.GuildId);
        var expiredAttacks = faction.TrackedAttacks
            .Where(a => DateTime.UtcNow - a.Timestamp > TimeSpan.FromMinutes(5))
            .ToImmutableList();

        await Parallel.ForEachAsync(expiredAttacks,
            new ParallelOptions { MaxDegreeOfParallelism = 5, CancellationToken = ct },
            async (expiredAttack, token) =>
            {
                var message = await client.GetMessageAsync(config!.NotificationChannelId!.Value,
                    expiredAttack.MessageId, cancellationToken: token);
                await client.ModifyMessageAsync(config.NotificationChannelId!.Value, expiredAttack.MessageId,
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
                    }, cancellationToken: token);
            });

        var expiredAttacksHashSet = expiredAttacks.Select(e => e.Id).ToHashSet();
        faction.TrackedAttacks.RemoveAll(a => expiredAttacksHashSet.Contains(a.Id));
    }
}