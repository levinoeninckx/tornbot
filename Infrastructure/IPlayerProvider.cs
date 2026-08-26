using TornBot.Bot.Domain.Models;

namespace TornBot.Bot.Infrastructure;

public interface IPlayerProvider
{
    public Task<Player?> GetPlayerByTornIdAsync(int tornId, ApiKey apiKey, FFScouterApiKey fFScouterApiKey, TornStatApiKey tornStatApiKey);
}
