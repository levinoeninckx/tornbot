using System.Text.Json;
using discordBotTest.Features.Chains;
using discordBotTest.Shared;
using FactionBot.Infrastructure.TornApi.Models;
using TornBot.Bot.Infrastructure.TornApi.Models;
using TornBot.Bot.Shared;

namespace TornBot.Bot.Infrastructure.TornApi;

public class TornApiClient
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ApiKeyService _apiKeyService;

    public TornApiClient(ApiKeyService apiKeyService, HttpClient httpClient)
    {
        _http = httpClient;

        _apiKeyService = apiKeyService;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    private async Task<T> GetAsync<T>(string endpoint, CancellationToken ct = default)
    {
        var apiKey = _apiKeyService.GetApiKey();

        if (apiKey == null)
        {
            throw new Exception("No API key set");
        }
        
        using var response = await _http.GetAsync($"{endpoint}?key={apiKey}", ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new Exception($"Torn API error: {response.StatusCode} - {body}");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct);

        var result = await JsonSerializer.DeserializeAsync<T>(stream, _jsonOptions, ct);

        if (result == null)
            throw new Exception("Failed to deserialize Torn API response");

        return result;
    }
    
    public async Task<ChainState> GetChainStateAsync(CancellationToken ct = default)
    {
        return await GetAsync<ChainState>("faction/chain", ct);
    }

    public async Task<Profile> GetUserProfileByDiscordId(ulong discordId, CancellationToken ct = default)
    {
        var userDiscordResponse = await GetAsync<UserDiscordResponse>($"user/{discordId}/discord", ct);

        var userBasicResponse = await GetAsync<UserBasicResponse>($"user/{userDiscordResponse.Discord.UserId}/basic", ct);

        return userBasicResponse.Profile;
    }
    
    public Task<FactionRankedWarsResponse> GetRankedWarsAsync(
        int factionId,
        CancellationToken ct = default)
    {
        return GetAsync<FactionRankedWarsResponse>(
            $"faction/{factionId}/rankedwars",
            ct);
    }
    
    public Task<RankedWarResponse> GetRankedWarAsync(
        int warId,
        CancellationToken ct = default)
    {
        return GetAsync<RankedWarResponse>(
            $"torn/rankedwars/{warId}",
            ct);
    }
    
    public Task<RankedWarReportResponse> GetRankedWarReportAsync(
        int warId,
        CancellationToken ct = default)
    {
        return GetAsync<RankedWarReportResponse>(
            $"torn/rankedwars/{warId}/report",
            ct);
    }
    
    public Task<FactionMembersResponse> GetFactionMembersAsync(int factionId, CancellationToken ct = default)
    {
        return GetAsync<FactionMembersResponse>($"faction/{factionId}/members", ct);
    }
    
    public Task<UserResponse> GetUserAsync(
        int userId,
        CancellationToken ct = default)
    {
        return GetAsync<UserResponse>(
            $"user/{userId}",
            ct);
    }
    
    public async Task<List<UserResponse>> GetUsersAsync(
        IEnumerable<int> userIds,
        CancellationToken ct = default)
    {
        var results = new List<UserResponse>();

        foreach (var id in userIds)
        {
            results.Add(await GetUserAsync(id, ct));

            // Torn rate limit safety
            await Task.Delay(600, ct);
        }

        return results;
    }
    
    public async Task<bool> ValidateKeyAsync(string key, CancellationToken ct = default)
    {
        using var response = await _http.GetAsync($"key/info?key={key}", ct);
        
        return response.IsSuccessStatusCode;
    }
}