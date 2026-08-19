using System.Collections.Immutable;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using TornBot.Bot.Domain.Models;
using TornBot.Bot.Features.Retaliation.Models;
using TornBot.Bot.Infrastructure;
using TornBot.Bot.Shared;
using AttackResult = TornBot.Bot.Domain.Enums.AttackResult;

namespace TornBot.Bot.Features.Retaliation;

public class AttackService(
    HttpClient httpClient,
    IPlayerProvider playerProvider,
    ApiKeyService apiKeyService,
    ILogger<AttackService> logger) : IAttackService
{
    public async Task<IReadOnlyList<Attack>> GetOutgoingAttacksByIdAsync(int factionId)
    {
        var key = await apiKeyService.GetLimitedApiKeyAsync(factionId, hasFactionAccess: true);
        if (key == null)
        {
            logger.LogInformation("No available limited key with faction api access");
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

        return attacksFullResponse.Attacks
            .Select(a => new Attack
            {
                Id = (ulong)a.Id,
                AttackerId = a.Attacker?.Id,
                DefenderId = a.Defender.Id,
                Result = Enum.Parse<AttackResult>(a.Result.ToString()),
                Timestamp = DateTimeOffset.FromUnixTimeSeconds(a.Ended).UtcDateTime
            })
            .ToImmutableList();
    }

    public async Task<IReadOnlyList<Attack>> GetIncomingAttacksByIdAsync(int factionId)
    {
        var key = await apiKeyService.GetLimitedApiKeyAsync(factionId, hasFactionAccess: true);
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

        return attacksFullResponse.Attacks
            .Select(a => new Attack
            {
                Id = (ulong)a.Id,
                AttackerId = a.Attacker?.Id,
                AttackerFactionId = a.Attacker?.FactionId,
                DefenderId = a.Defender.Id,
                DefenderFactionId = a.Defender.FactionId,
                Result = Enum.Parse<AttackResult>(a.Result.ToString()),
                Timestamp = DateTimeOffset.FromUnixTimeSeconds(a.Ended).UtcDateTime
            })
            .ToImmutableList();
    }
}