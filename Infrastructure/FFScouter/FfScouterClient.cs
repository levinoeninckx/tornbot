using System.Net.Http.Json;
using TornBot.Bot.Infrastructure.FFScouter.Models;
using TornBot.Bot.Shared;

namespace TornBot.Bot.Infrastructure.FFScouter;

public class FfScouterClient(HttpClient client, ApiKeyService keyService)
{
    public async Task<PlayerStats?> GetPlayerStats(params IEnumerable<int> playerIds)
    {
        var apiKey = await keyService.GetFfScouterApiKeyAsync();
        var playerStats = await client.GetFromJsonAsync<PlayerStats[]>($"get-stats?key={apiKey}&targets={string.Join(',', playerIds)}");

        return playerStats?[0];
    }
}