using System.Net.Http.Json;
using TornBot.Bot.Infrastructure.FFScouter.Models;
using TornBot.Bot.Shared;

namespace TornBot.Bot.Infrastructure.FFScouter;

public class FfScouterClient(HttpClient client, ApiKeyService keyService)
{
    public async Task<FfPlayerStats?> GetPlayerStats(params IEnumerable<int> playerIds)
    {
        var apiKey = await keyService.GetFfScouterApiKeyAsync();
        var playerStats = await client.GetFromJsonAsync<FfPlayerStats[]>($"get-stats?key={apiKey}&targets={string.Join(',', playerIds)}");

        return playerStats?[0];
    }

    public async Task<bool> IsApiKeyValid(string apiKey)
    {
        try
        {
            var response = await client.GetFromJsonAsync<ApiKey>($"check-key?key={apiKey}");
            return response is { IsRegistered: true };
        }
        catch(Exception)
        {
            return false;
        }
    }
}