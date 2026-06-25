using System.Net.Http.Json;
using System.Text.Json;
using discordBotTest.Features.Chains;
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

    private async Task<T> GetAsync<T>(string endpoint, string key, CancellationToken ct = default)
    {
        using var response = await _http.GetAsync($"{endpoint}?key={key}", ct);
        
        var bodyString = await response.Content.ReadAsStringAsync(ct);
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
        var key = await _apiKeyService.GetPublicApiKeyAsync();
        if (key == null)
        {
            return null;
        }
        return await GetAsync<ChainState>("faction/chain", key, ct);
    }

    public async Task<Profile> GetUserProfileByDiscordId(ulong discordId, CancellationToken ct = default)
    {
        var key = await _apiKeyService.GetPublicApiKeyAsync();
        if (key == null)
        {
            return null;
        }
        var userDiscordResponse = await GetAsync<UserDiscordResponse>($"user/{discordId}/discord", key, ct);
        
        var userBasicResponse =
            await GetAsync<UserBasicResponse>($"user/{userDiscordResponse.Discord.UserId}/basic", key, ct);

        return userBasicResponse.Profile;
    }

    public async Task<Faction?> GetUserFactionAsync(int userId, CancellationToken ct = default)
    {
        var key = await _apiKeyService.GetPublicApiKeyAsync();
        if (key == null)
        {
            return null;
        }
        var userFacionResponse = await GetAsync<UserFactionResponse>($"user/{userId}/faction", key, ct);

        if (userFacionResponse.Faction == null) return null;
        
        return userFacionResponse.Faction;
    }

    public async Task<UserResponse> GetUserAsync(ulong userId, CancellationToken ct = default)
    {
        var tornProfile = await GetUserProfileByDiscordId(userId, ct);
        
        var key = await _apiKeyService.GetPublicApiKeyAsync();
        if (key == null)
        {
            return null;
        }
        return await GetAsync<UserResponse>($"user/{tornProfile.Id}", key, ct);
    }

    public async Task<KeyInfo?> GetKeyInfoAsync(string key, CancellationToken ct = default)
    {
        using var response = await _http.GetAsync($"key/info?key={key}", ct);
        
        var keyInfoResponse = await response.Content.ReadFromJsonAsync<KeyInfoResponse>(cancellationToken: ct);

        return keyInfoResponse?.Info;
    }
    
    public async Task<Factionbasic?> GetFactionBasicAsync(int factionId, CancellationToken ct = default)
    {
        var key = await _apiKeyService.GetPublicApiKeyAsync();
        if (key == null)
        {
            return null;
        }
        var response = await GetAsync<FactionBasicResponse>($"faction/{factionId}/basic", key, ct);
        
        return response.Basic;
    }
    
    public async Task<FactionMemberBalance?> GetMemberFactionBalanceByIdAsync(ulong userId, CancellationToken ct = default)
    {
        var apiKey = await _apiKeyService.GetLimitedApiKeyAsync(hasFactionAccess: true);
        if(apiKey == null)
        {
            return null;
        }
        
        var response = await GetAsync<FactionBalanceResponse>("faction/balance", apiKey.Key, ct);

        var tornProfile = await GetUserProfileByDiscordId(userId, ct);
        var memberBalance = response.Balance.Members.FirstOrDefault(x => x.Id == tornProfile.Id);
        
        return memberBalance;
    }
}