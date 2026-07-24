using TornBot.Domain.Models;

namespace TornBot.Application.Contracts;

public interface IPlayerProvider
{
    public Task<Player?> GetPlayerById(int playerId, ApiKey apiKey);
    public Task<Player?> GetPlayerByDiscordId(ulong discordId, ApiKey apiKey);
}