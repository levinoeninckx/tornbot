using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Rest;
using Quartz;
using TornBot.Bot.Infrastructure;

namespace TornBot.Bot.Features.Retaliation;

public class CheckExpiredRetals(IDbContextFactory<TornbotContext> contextFactory, ModuleConfigRepository repository, RestClient client, ILogger<CheckExpiredRetals> logger) : IJob
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
                a.Timestamp - DateTime.UtcNow > TimeSpan.FromMinutes(5)).ToImmutableList();
            foreach (var expiredAttack in expiredAttacks)
            {
                await client.ModifyMessageAsync(config!.ChannelId!.Value, expiredAttack.MessageId, messageProperties =>
                {
                    messageProperties.Embeds =
                    [
                        new EmbedProperties
                        {
                            Title = "Retal Expired",
                            Color = new Color(255, 0, 0),
                        }
                    ];
                    messageProperties.Components = [];
                });
            }
            
            var expiredAttacksHashSet = expiredAttacks.Select(e => e.Id).ToHashSet();
            faction.TrackedAttacks.RemoveAll(a => expiredAttacksHashSet.Contains(a.Id));
        }
        
        await dbContext.SaveChangesAsync();
    }
}