using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using discordBotTest.Features.Chains;
using TornBot.Bot.Domain.Enums;
using TornBot.Bot.Domain.Models;
using TornBot.Bot.Infrastructure.TornApi.Models;
using TornBot.Bot.Shared;
using Faction = FactionBot.Infrastructure.TornApi.Models.Faction;

namespace TornBot.Bot.Infrastructure.TornApi;

public class TornApiClient(HttpClient httpClient, ApiKeyService apiKeyService)
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<FactionCrime[]> GetFactionCrimesAsync(ApiKey key, CancellationToken ct = default)
    {
        if(key.AccessLevel != AccessLevel.Minimal || !key.HasFactionAccess)
        {
            return [];
        }
        
        var response = await GetAsync<FactionCrimesResponse>( "faction/crimes", key.Key, ct);
        if (response.Crimes == null)
        {
            throw new InvalidOperationException();
        }
        
        return response.Crimes;
    }

    public async Task<IReadOnlyList<FactionMember>> GetFactionMembersByFactionIdAsync(int factionId, CancellationToken ct = default)
    {
        var key = await apiKeyService.GetPublicApiKeyAsync();
        if (key == null)
        {
            return [];
        }
        
        var response = await GetAsync<FactionMembersResponse>($"faction/{factionId}/members", key, ct);
        if (response.Members == null)
        {
            throw new InvalidOperationException();
        }

        return response.Members;
    }
    public async Task<FactionCrime[]> GetAllFactionCrimesAsync()
    {
        var key = await apiKeyService.GetMinimalApiKeyAsync(true);
        if (key == null)
        {
            return [];
        }

        const int limit = 100;
        const int batchSize = 10; // Number of parallel requests to make at once
        var allCrimes = new ConcurrentBag<FactionCrime>();
        int currentOffset = 0;
        bool hasReachedEnd = false;

        while (!hasReachedEnd)
        {
            var tasks = new List<Task<FactionCrimesResponse>>();
            
            // Prepare a batch of requests
            for (int i = 0; i < batchSize; i++)
            {
                int offset = currentOffset + (i * limit);
                var queryParams = $"offset={offset}&limit={limit}";
                tasks.Add(GetAsync<FactionCrimesResponse>("faction/crimes", key.Key, queryParamters: queryParams, ct: CancellationToken.None));
            }

            // Execute the batch in parallel
            var results = await Task.WhenAll(tasks);

            foreach (var response in results)
            {
                if (response.Crimes == null || response.Crimes.Length == 0)
                {
                    hasReachedEnd = true;
                    continue;
                }

                foreach (var crime in response.Crimes)
                {
                    allCrimes.Add(crime);
                }

                // If a page is not full, we've reached the end
                if (response.Crimes.Length < limit)
                {
                    hasReachedEnd = true;
                }
            }

            if (!hasReachedEnd)
            {
                currentOffset += batchSize * limit;
            }
        }
        
        return allCrimes.ToArray();
    }
    
    private async Task<T> GetAsync<T>(string endpoint, string key, CancellationToken ct = default, string queryParamters = "")
    {
        var url = $"{endpoint}?key={key}";
        if (!string.IsNullOrEmpty(queryParamters))
        {
            url = $"{url}&{queryParamters}";
        }
        using var response = await httpClient.GetAsync(url, ct);
        
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
        var key = await apiKeyService.GetPublicApiKeyAsync();
        if (key == null)
        {
            return null;
        }
        return await GetAsync<ChainState>("faction/chain", key, ct);
    }

    public async Task<Profile> GetUserProfileByDiscordId(ulong discordId, CancellationToken ct = default)
    {
        var key = await apiKeyService.GetPublicApiKeyAsync();
        if (key == null)
        {
            return null;
        }
        var userDiscordResponse = await GetAsync<UserDiscordResponse>($"user/{discordId}/discord", key, ct);
        
        var userBasicResponse =
            await GetAsync<UserBasicResponse>($"user/{userDiscordResponse.Discord.UserId}/basic", key, ct);

        return userBasicResponse.Profile;
    }
    
    public async Task<Profile?> GetUserProfileById(int userId, CancellationToken ct = default)
    {
        var key = await apiKeyService.GetPublicApiKeyAsync();
        if (key == null)
        {
            return null;
        }
        var userBasicResponse =
            await GetAsync<UserBasicResponse>($"user/{userId}/basic", key, ct);

        return userBasicResponse.Profile;
    }

    public async Task<Faction?> GetUserFactionAsync(int userId, CancellationToken ct = default)
    {
        var key = await apiKeyService.GetPublicApiKeyAsync();
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
        
        var key = await apiKeyService.GetPublicApiKeyAsync();
        if (key == null)
        {
            return null;
        }
        return await GetAsync<UserResponse>($"user/{tornProfile.Id}", key, ct);
    }

    public async Task<KeyInfo?> GetKeyInfoAsync(string key, CancellationToken ct = default)
    {
        using var response = await httpClient.GetAsync($"key/info?key={key}", ct);
        
        var keyInfoResponse = await response.Content.ReadFromJsonAsync<KeyInfoResponse>(cancellationToken: ct);

        return keyInfoResponse?.Info;
    }
    
    public async Task<Factionbasic?> GetFactionBasicAsync(int factionId, CancellationToken ct = default)
    {
        var key = await apiKeyService.GetPublicApiKeyAsync();
        if (key == null)
        {
            return null;
        }
        var response = await GetAsync<FactionBasicResponse>($"faction/{factionId}/basic", key, ct);
        
        return response.Basic;
    }
    
    public async Task<FactionMemberBalance?> GetMemberFactionBalanceByIdAsync(ulong guildId, ulong userId, CancellationToken ct = default)
    {
        var apiKey = await apiKeyService.GetLimitedApiKeyAsync(guildId, hasFactionAccess: true);
        if(apiKey == null)
        {
            return null;
        }
        
        var response = await GetAsync<FactionBalanceResponse>("faction/balance", apiKey.Key, ct);

        var tornProfile = await GetUserProfileByDiscordId(userId, ct);
        var memberBalance = response.Balance.Members.FirstOrDefault(x => x.Id == tornProfile.Id);
        
        return memberBalance;
    }
    
    public async Task<IReadOnlyList<TornItem>?> GetItemsInfoAsync(IEnumerable<int> itemIds, CancellationToken ct = default)
    {
        var key = await apiKeyService.GetPublicApiKeyAsync();
        if (key == null)
        {
            return null;
        }

        var response = await GetAsync<TornItemsResponse>($"torn/{string.Join(',', itemIds)}/items", key, ct);

        return response.Items;
    }
}