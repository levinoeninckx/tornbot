using TornBot.Application.Contracts;
using TornBot.Domain.Models;

namespace TornBot.Application.Services;

public class PlayerService(IApiKeyService apiKeyService, IPlayerProvider playerProvider)
{
    public async Task<Player?> GetPlayerByDiscordId(ulong discordId)
    {
        var apiKey = await apiKeyService.GetPublicKeyByFactionIdAsync(1);
        if (apiKey == null)
        {
            return null;
        }

        return await playerProvider.GetPlayerByDiscordId(discordId, apiKey);
    }
}