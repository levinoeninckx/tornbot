using Microsoft.EntityFrameworkCore;
using TornBot.Infrastructure.Persistence;

namespace TornBot.Bot.Shared;

public class FactionService(IDbContextFactory<TornbotContext> contextFactory)
{
    public async Task<bool> AddFactionAsync(int factionId, ulong guildId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var faction = new Faction
        {
            FactionId = factionId,
            GuildId = guildId,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            await context.Factions.AddAsync(faction);
            await context.SaveChangesAsync();

            return true;
        }
        catch (Exception e)
        {
            return false;
        }
    }

    public async Task<Faction?> GetFactionByGuildIdAsync(ulong guildId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Factions.SingleOrDefaultAsync(f => f.GuildId == guildId);
    }
}