using Microsoft.EntityFrameworkCore;
using TornBot.Bot.Domain.Models;
using TornBot.Bot.Infrastructure;

namespace TornBot.Bot.Shared;

public class FactionService(TornbotContext context)
{
    public async Task<bool> AddFactionAsync(int factionId, ulong guildId)
    {
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
        return await context.Factions.SingleOrDefaultAsync(f => f.GuildId == guildId);
    }
}