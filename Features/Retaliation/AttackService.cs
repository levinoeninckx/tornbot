using System.Collections.Immutable;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TornBot.Bot.Domain.Enums;
using TornBot.Bot.Domain.Models;
using TornBot.Bot.Features.Retaliation.Models;
using TornBot.Bot.Infrastructure;
using AttackResult = TornBot.Bot.Domain.Enums.AttackResult;

namespace TornBot.Bot.Features.Retaliation;

public class AttackService(
    HttpClient httpClient,
    IDbContextFactory<TornbotContext> contextFactory,
    ILogger<AttackService> logger) : IAttackService
{
    public async Task<IReadOnlyList<Attack>> GetOutgoingAttacksByIdAsync(int factionId, ApiKey limitedKey)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var faction = await context.Factions
            .Include(f => f.ApiKeys)
            .SingleOrDefaultAsync(f => f.FactionId == factionId);

        if (limitedKey.AccessLevel is not AccessLevel.LimitedAccess)
        {
            logger.LogWarning("Provided key does not have limited access");
            return [];
        }
        if (!limitedKey.HasFactionAccess)
        {
            logger.LogWarning("Provided key does not have faction api access");
            return [];
        }

        var response = await httpClient.GetAsync($"?filters=outgoing&limit=1000&sort=DESC&key={limitedKey}");
        limitedKey.IncreaseUsage();
        await context.SaveChangesAsync();

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

    public async Task<IReadOnlyList<Attack>> GetIncomingAttacksByIdAsync(int factionId, ApiKey limitedKey)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var faction = await context.Factions
            .Include(f => f.ApiKeys)
            .SingleOrDefaultAsync(f => f.FactionId == factionId);

        var key = faction?.GetKey(AccessLevel.LimitedAccess, requireFactionAccess: true);
        if (key == null)
        {
            logger.LogInformation("No available limited key with faction api access");
            return [];
        }

        var response = await httpClient.GetAsync($"?filters=incoming&limit=1000&sort=DESC&key={key}");
        key.IncreaseUsage();
        await context.SaveChangesAsync();

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
