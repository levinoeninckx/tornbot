using TornBot.Bot.Domain.Models;

namespace TornBot.Bot.Infrastructure;

public interface IPlayerProvider
{
    public Task<Player?> GetPlayerByTornIdAsync(int tornId, ulong guildId);
    public Task<Player?> GetPlayerByDiscordIdAsync(ulong discordId, ulong guildId);
}