using System.Net.Http.Json;
using TornBot.Bot.Infrastructure.FFScouter.Models;

namespace TornBot.Bot.Infrastructure.FFScouter;

public class FfScouterClient(HttpClient client)
{
    public async Task<FfPlayerStats?> GetPlayerStats(string key, params IEnumerable<int> playerIds)
    {
        var playerStats =
            await client.GetFromJsonAsync<FfPlayerStats[]>(
                $"get-stats?key={key}&targets={string.Join(',', playerIds)}");

        return playerStats?[0];
    }

    public async Task<bool> IsApiKeyValid(string apiKey)
    {
        try
        {
            var response = await client.GetFromJsonAsync<ApiKey>($"check-key?key={apiKey}");
            return response is { IsRegistered: true };
        }
        catch (Exception)
        {
            return false;
        }
    }
}