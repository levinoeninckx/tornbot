using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TornBot.Bot.Infrastructure.TornApi.Models;
namespace TornBot.Bot.Infrastructure.TornApi;

public class TornApiClient(HttpClient httpClient, ILogger<TornApiClient> logger)
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public Task<ImmutableList<FactionCrime>?> GetAvailableCrimesAsync(string apiKey,
        CancellationToken ct = default)
        => GetCrimesByCategoryAsync(apiKey, "available", ct);

    public Task<ImmutableList<FactionCrime>?> GetCompletedCrimesAsync(string apiKey,
        CancellationToken ct = default)
        => GetCrimesByCategoryAsync(apiKey, "completed", ct);

    private async Task<ImmutableList<FactionCrime>?> GetCrimesByCategoryAsync(string apiKey, string category,
        CancellationToken ct)
    {
        try
        {
            var response = await GetAsync<FactionCrimesResponse>("faction/crimes", apiKey, ct,
                $"cat={category}&limit=50&sort=DESC");
            if (response.Crimes == null)
            {
                logger.LogError("Response was empty, something went wrong accessing the torn api");
                return null;
            }

            return response.Crimes.ToImmutableList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Something went wrong while processing the request to the torn api");
            return null;
        }
    }

    public async Task<IReadOnlyList<FactionMember>> GetFactionMembersByFactionIdAsync(int factionId, string apiKey,
        CancellationToken ct = default)
    {
        var response = await GetAsync<FactionMembersResponse>($"faction/{factionId}/members", apiKey, ct);
        if (response.Members == null)
        {
            throw new InvalidOperationException();
        }

        return response.Members;
    }

    public async Task<FactionCrime[]> GetAllFactionCrimesAsync(string apiKey)
    {
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
                tasks.Add(GetAsync<FactionCrimesResponse>("faction/crimes", apiKey, queryParameters: queryParams,
                    ct: CancellationToken.None));
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

    private async Task<T> GetAsync<T>(string endpoint, string key, CancellationToken ct = default,
        string queryParameters = "")
    {
        var url = $"{endpoint}?key={key}";
        if (!string.IsNullOrEmpty(queryParameters))
        {
            url = $"{url}&{queryParameters}";
        }

        using var response = await httpClient.GetAsync(url, ct);

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

    public async Task<ChainState> GetChainStateAsync(string apiKey, CancellationToken ct = default)
    {
        return await GetAsync<ChainState>("faction/chain", apiKey, ct);
    }

    public async Task<Profile?> GetUserProfileByDiscordId(ulong discordId, string apiKey,
        CancellationToken ct = default)
    {
        var userDiscordResponse = await GetAsync<UserDiscordResponse>($"user/{discordId}/discord", apiKey, ct);

        var userBasicResponse =
            await GetAsync<UserBasicResponse>($"user/{userDiscordResponse.Discord.UserId}/basic", apiKey, ct);

        return userBasicResponse.Profile;
    }

    public async Task<Profile?> GetUserProfileById(int userId, string apiKey, CancellationToken ct = default)
    {
        var userBasicResponse =
            await GetAsync<UserBasicResponse>($"user/{userId}/basic", apiKey, ct);

        return userBasicResponse.Profile;
    }

    public async Task<TornFaction?> GetUserFactionAsync(int userId, string apiKey, CancellationToken ct = default)
    {
        var userFacionResponse = await GetAsync<UserFactionResponse>($"user/{userId}/faction", apiKey, ct);

        if (userFacionResponse.Faction == null) return null;

        return userFacionResponse.Faction;
    }

    public async Task<UserResponse> GetUserAsync(ulong userId, string apiKey, CancellationToken ct = default)
    {
        var tornProfile = await GetUserProfileByDiscordId(userId, apiKey, ct);

        return await GetAsync<UserResponse>($"user/{tornProfile.Id}", apiKey, ct);
    }

    public async Task<KeyInfo?> GetKeyInfoAsync(string key, CancellationToken ct = default)
    {
        using var response = await httpClient.GetAsync($"key/info?key={key}", ct);

        var keyInfoResponse = await response.Content.ReadFromJsonAsync<KeyInfoResponse>(cancellationToken: ct);

        return keyInfoResponse?.Info;
    }

    public async Task<Factionbasic?> GetFactionBasicAsync(int factionId, string apiKey, CancellationToken ct = default)
    {
        var response = await GetAsync<FactionBasicResponse>($"faction/{factionId}/basic", apiKey, ct);

        return response.Basic;
    }

    public async Task<FactionMemberBalance?> GetMemberFactionBalanceByIdAsync(int factionId, ulong userId,
        string apiKey, CancellationToken ct = default)
    {
        var response = await GetAsync<FactionBalanceResponse>("faction/balance", apiKey, ct);

        var tornProfile = await GetUserProfileByDiscordId(userId, apiKey, ct);
        var memberBalance = response.Balance.Members.FirstOrDefault(x => x.Id == tornProfile.Id);

        return memberBalance;
    }

    public async Task<IReadOnlyList<TornItem>?> GetItemsInfoAsync(IEnumerable<int> itemIds, string apiKey,
        CancellationToken ct = default)
    {
        var ids = itemIds.ToList();
        if (ids.Count == 0)
            return [];

        var response = await GetAsync<TornItemsResponse>($"torn/{string.Join(',', ids)}/items", apiKey, ct);

        return response.Items;
    }
}