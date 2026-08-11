using TornBot.Bot.Domain.Models;

namespace TornBot.Bot.Infrastructure;

public interface IPlayerProvider
{
    public Task<Player?> GetPlayerByTornIdAsync(int tornId, int factionId);
    public Task<Player?> GetPlayerByDiscordIdAsync(ulong discordId, int factionId);
}