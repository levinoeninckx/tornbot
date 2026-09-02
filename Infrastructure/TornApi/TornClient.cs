using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TornBot.Bot.Domain.Enums;
using TornBot.Bot.Domain.Models;
using TornBot.Bot.Infrastructure.TornApi.Models;

namespace TornBot.Bot.Infrastructure.TornApi;

public class TornClient(HttpClient httpClient, ILogger<TornClient> logger)
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public Task<ImmutableList<FactionCrime>?> GetAvailableCrimesAsync(ApiKey limitedKey,
        CancellationToken ct = default)
        => GetCrimesByCategoryAsync(limitedKey, "available", ct);

    public Task<ImmutableList<FactionCrime>?> GetCompletedCrimesAsync(ApiKey limitedKey,
        CancellationToken ct = default)
        => GetCrimesByCategoryAsync(limitedKey, "completed", ct);

    private async Task<ImmutableList<FactionCrime>?> GetCrimesByCategoryAsync(ApiKey minimal, string category,
        CancellationToken ct)
    {
        try
        {
            if (minimal.AccessLevel != AccessLevel.Minimal)
            {
                logger.LogWarning("Provided key {key} does not have limited access", minimal.Key);
                return null;
            }

            var response = await GetAsync<FactionCrimesResponse>("faction/crimes", minimal.Key, ct,
                $"cat={category}&limit=50&sort=DESC");
            if (response.Crimes == null)
            {
                logger.LogError("Response was empty, something went wrong accessing the torn api");
                return null;
            }

            minimal.IncreaseUsage();
            return response.Crimes.ToImmutableList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Something went wrong while processing the request to the torn api");
            return null;
        }
    }

    public async Task<IReadOnlyList<FactionMember>?> GetFactionMembersByFactionIdAsync(int factionId, ApiKey publicKey,
        CancellationToken ct = default)
    {
        var response = await GetAsync<FactionMembersResponse>($"faction/{factionId}/members", publicKey.Key, ct);
        if (response is null)
        {
            logger.LogWarning("Response was empty, something went wrong accessing the torn api");
            return null;
        }

        publicKey.IncreaseUsage();

        return response.Members
            .Select(m => new FactionMember
            {
                Id = m.Id,
                Name = m.Name,
                Level = m.Level,
                DaysInFaction = m.DaysInFaction,
                ActivityStatus = Enum.Parse<ActivityStatus>(m.LastAction.Status.ToString()),
                CanEarlyDischarge = m.HasEarlyDischarge,
                CurrentState = Enum.Parse<PlayerState>(m.Status.State.ToString()),
                InOc = m.IsInOc,
                IsRevivable = m.IsRevivable
            })
            .ToImmutableList();
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

    private async Task<T?> GetAsync<T>(string endpoint, string key, CancellationToken ct = default,
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

    public async Task<UserFaction?> GetUserFactionAsync(int userId, string apiKey, CancellationToken ct = default)
    {
        var userFacionResponse = await GetAsync<UserFactionResponse>($"user/{userId}/faction", apiKey, ct);

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

    public async Task<FactionBasic?> GetFactionBasicAsync(int factionId, ApiKey publicKey,
        CancellationToken ct = default)
    {
        try
        {
            var response = await GetAsync<FactionBasicResponse>($"faction/{factionId}/basic", publicKey.Key, ct);

            if (response is null)
                return null;

            var factionBasic = new FactionBasic
            {
                Id = response.Basic.Id,
                Name = response.Basic.Name,
                MemberCount = response.Basic.Members,
                Rank = $"{response.Basic.Rank.Name} {response.Basic.Rank.Position}",
                Respect = Convert.ToUInt32(response.Basic.Respect)
            };

            logger.LogInformation("Found faction basic for {id}", factionId);
            return factionBasic;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to get faction with id {factionId}", factionId);
            return null;
        }
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