using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using TornBot.Bot.Infrastructure.TornStats.Models;
namespace TornBot.Bot.Infrastructure.TornStats;

public class TornStatClient(HttpClient client, ILogger<TornStatClient> logger)
{
    public async Task<ProfileDetails?> GetSpyProfileDetailsById(int playerId, string apiKey)
    {
        var endpoint = $"{apiKey}/spy/user/{playerId}";

        try
        {
            var response = await client.GetAsync(endpoint);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("TornStats API resposne for playerId {playerId}: {statusCode}, reason: {reason}",
                    playerId, response.StatusCode, response.ReasonPhrase);
                return null;
            }

            var profileDetails = await response.Content.ReadFromJsonAsync<ProfileDetails>();

            return profileDetails;
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
            var response = await client.GetAsync(key);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogInformation("TornStats API resposne for checking key {key}: {statusCode}, reason: {reason}",
                    key, response.StatusCode, response.ReasonPhrase);
                return false;
            }

            var keyCheck = await response.Content.ReadFromJsonAsync<KeyCheck>();

            if (keyCheck is null)
            {
                logger.LogWarning("TornStats API returned null for checking key {key}", key);
                return false;
            }

            logger.LogInformation("Key {key} status: {status} with message {message}", key, keyCheck.Status,
                keyCheck.Message);
            return keyCheck.Status;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "something went wrong while contacting the tornstats api");
            return false;
        }
    }
}