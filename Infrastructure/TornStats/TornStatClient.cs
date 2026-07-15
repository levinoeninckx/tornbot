using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using TornBot.Bot.Infrastructure.TornStats.Models;
using TornBot.Bot.Shared;

namespace TornBot.Bot.Infrastructure.TornStats;

public class TornStatClient(HttpClient client, ApiKeyService keyService, ILogger<TornStatClient> logger)
{
    public async Task<ProfileDetails?> GetSpyProfileDetailsById(int playerId)
    {
        var key = await keyService.GetTornStatsApiKeyAsync();
        var endpoint = $"{key}/spy/user/{playerId}";
        
        try
        {
            return await client.GetFromJsonAsync<ProfileDetails>(endpoint);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "something went wrong while contacting the tornstats api");
            return null;
        }
    }

    public async Task<bool> IsKeyValidAsync(string key)
    {
        try
        {
            var keyCheck = await client.GetFromJsonAsync<KeyCheck>(key);

            return keyCheck is not null && keyCheck.Status;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "something went wrong while contacting the tornstats api");
            return false;
        }
    }
}