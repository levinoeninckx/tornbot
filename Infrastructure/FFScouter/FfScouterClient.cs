using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using TornBot.Bot.Infrastructure.FFScouter.Models;

namespace TornBot.Bot.Infrastructure.FFScouter;

public class FfScouterClient(HttpClient client, ILogger<FfScouterClient> logger)
{
    public async Task<FfPlayerStats?> GetPlayerStats(string key, params IEnumerable<int> playerIds)
    {
        try
        {

            var response = await client.GetAsync($"get-stats?key={key}&targets={string.Join(',', playerIds)}");
            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("FFScouter api returned an error: statusCode {statusCode} - Body {body}", response.StatusCode, await response.Content.ReadAsStringAsync());
                return null;
            }

            var playerStats = await response.Content.ReadFromJsonAsync<FfPlayerStats[]>();
            if (playerStats is null)
            {
                logger.LogError("Failed to parse JSON from FFScouter API");
                return null;
            }

            logger.LogInformation("Got {amount} playerStats", playerStats.Count());

            return playerStats?[0];
        }

        catch (Exception ex)
        {
            logger.LogError(ex, "Something went wrong while processing the request to the FFScouter API");
            return null;
        }
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
