using System.Net.Http.Json;
using TornBot.Infrastructure.FFScouter.Dtos;
using TornBot.Infrastructure.FFScouter.Models;

namespace TornBot.Infrastructure.FFScouter;

public class FfScouterClient(HttpClient client)
{
    public async Task<PlayerStats?> GetPlayerStats(string key, params IEnumerable<int> playerIds)
    {
        var playerStats =
            await client.GetFromJsonAsync<PlayerStats[]>($"get-stats?key={key}&targets={string.Join(',', playerIds)}");

        return playerStats?[0];
    }

    public async Task<bool> IsApiKeyValid(string key)
    {
        try
        {
            var response = await client.GetFromJsonAsync<ApiKeyDto>($"check-key?key={key}");
            return response is { IsRegistered: true };
        }
        catch (Exception)
        {
            return false;
        }
    }
}