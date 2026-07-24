using Microsoft.Extensions.Logging;
using TornBot.Application.Contracts;
using TornBot.Domain.Models;
using TornBot.Infrastructure.TornApi;
using TornBot.Infrastructure.TornApi.Mappers;

namespace TornBot.Infrastructure.Services;

public class PlayerProvider(TornApiClient tornClient, ILogger<PlayerProvider> logger) : IPlayerProvider
{
    public async Task<Player?> GetPlayerById(int playerId, ApiKey apiKey)
    {
        var profile = await tornClient.GetUserProfileById(playerId, apiKey.Key);
        if (profile == null)
        {
            logger.LogWarning("No profile found for player id {playerId}", playerId);
            return null;
        }

        var player = PlayerMapper.MapToDomain(profile);

        return player;
    }

    public async Task<Player?> GetPlayerByDiscordId(ulong discordId, ApiKey apiKey)
    {
        var profile = await tornClient.GetUserProfileByDiscordId(discordId, apiKey.Key);
        if (profile == null)
        {
            logger.LogWarning("No profile found for discord id {discordId}", discordId);
            return null;
        }

        var player = PlayerMapper.MapToDomain(profile);

        return player;
    }
}