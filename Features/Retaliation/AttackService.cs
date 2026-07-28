using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using TornBot.Bot.Features.Retaliation.Models;
using TornBot.Bot.Shared;

namespace TornBot.Bot.Features.Retaliation;

public class AttackService(HttpClient httpClient, ApiKeyService apiKeyService, ILogger<AttackService> logger)
{
    public async Task<IReadOnlyList<AttackFull>> GetIncomingAttacks(ulong guildId)
    {
        var key = await apiKeyService.GetLimitedApiKeyAsync(guildId, hasFactionAccess: true);
        if (key == null)
        {
            logger.LogInformation("No available limited key with faction api access");
            return [];
        }

        var response = await httpClient.GetAsync($"?filters=incoming&limit=1000&sort=DESC&key={key}");

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            logger.LogError("Torn API error: {StatusCode} - {Body}", response.StatusCode, body);
            return [];
        }

        var attacksFullResponse = await response.Content.ReadFromJsonAsync<AttacksFullResponse>();

        if (attacksFullResponse == null)
        {
            logger.LogError("Response was empty, something went wrong accessing the torn api");
            return [];
        }

        return attacksFullResponse.Attacks;
    }

    public async Task<IReadOnlyList<AttackFull>> GetOutgoingAttacks(ulong guildId)
    {
        var key = await apiKeyService.GetLimitedApiKeyAsync(guildId, hasFactionAccess: true);
        if (key == null)
        {
            logger.LogWarning($"No available limited key with faction api access");
            return [];
        }

        var response = await httpClient.GetAsync($"?filters=outgoing&limit=1000&sort=DESC&key={key}");

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            logger.LogError("Torn API error: {StatusCode} - {Body}", response.StatusCode, body);
            return [];
        }

        var attacksFullResponse = await response.Content.ReadFromJsonAsync<AttacksFullResponse>();

        if (attacksFullResponse == null)
        {
            logger.LogError("Response was empty, something went wrong accessing the torn api");
            return [];
        }

        return attacksFullResponse.Attacks;
    }
}