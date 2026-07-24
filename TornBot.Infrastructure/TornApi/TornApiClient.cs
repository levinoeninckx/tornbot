using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using discordBotTest.Features.Chains;
using TornBot.Bot.Infrastructure.TornApi.Models;
using Faction = FactionBot.Infrastructure.TornApi.Models.Faction;

namespace TornBot.Infrastructure.TornApi;

public class TornApiClient(HttpClient httpClient)
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<FactionCrime[]> GetFactionCrimesAsync(string key, CancellationToken ct = default)
    {
        var response = await GetAsync<FactionCrimesResponse>("faction/crimes", key, ct);
        if (response.Crimes == null)
        {
            throw new InvalidOperationException();
        }

        return response.Crimes;
    }

    public async Task<IReadOnlyList<FactionMember>> GetFactionMembersByFactionIdAsync(int factionId, string key,
        CancellationToken ct = default)
    {
        var response = await GetAsync<FactionMembersResponse>($"faction/{factionId}/members", key, ct);
        if (response.Members == null)
        {
            throw new InvalidOperationException();
        }

        return response.Members;
    }

    public async Task<FactionCrime[]> GetAllFactionCrimesAsync(string key, CancellationToken ct = default)
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
                tasks.Add(GetAsync<FactionCrimesResponse>("faction/crimes", key, queryParamters: queryParams,
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
        string queryParamters = "")
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

    public async Task<ChainState> GetChainStateAsync(string key, CancellationToken ct = default)
    {
        return await GetAsync<ChainState>("faction/chain", key, ct);
    }

    public async Task<Profile?> GetUserProfileByDiscordId(ulong discordId, string key, CancellationToken ct = default)
    {
        var userDiscordResponse = await GetAsync<UserDiscordResponse>($"user/{discordId}/discord", key, ct);

        var userBasicResponse =
            await GetAsync<UserBasicResponse>($"user/{userDiscordResponse.Discord.UserId}/basic", key, ct);

        return userBasicResponse.Profile;
    }

    public async Task<Profile?> GetUserProfileById(int userId, string key, CancellationToken ct = default)
    {
        var userBasicResponse =
            await GetAsync<UserBasicResponse>($"user/{userId}/basic", key, ct);

        return userBasicResponse.Profile;
    }

    public async Task<Faction?> GetUserFactionAsync(int userId, string key, CancellationToken ct = default)
    {
        var userFacionResponse = await GetAsync<UserFactionResponse>($"user/{userId}/faction", key, ct);

        if (userFacionResponse.Faction == null) return null;

        return userFacionResponse.Faction;
    }

    public async Task<UserResponse> GetUserAsync(ulong userId, string key, CancellationToken ct = default)
    {
        var tornProfile = await GetUserProfileByDiscordId(userId, key, ct);

        return await GetAsync<UserResponse>($"user/{tornProfile.Id}", key, ct);
    }

    public async Task<KeyInfo?> GetKeyInfoAsync(string key, CancellationToken ct = default)
    {
        using var response = await httpClient.GetAsync($"key/info?key={key}", ct);

        var keyInfoResponse = await response.Content.ReadFromJsonAsync<KeyInfoResponse>(cancellationToken: ct);

        return keyInfoResponse?.Info;
    }

    public async Task<Factionbasic?> GetFactionBasicAsync(int factionId, string key, CancellationToken ct = default)
    {
        var response = await GetAsync<FactionBasicResponse>($"faction/{factionId}/basic", key, ct);

        return response.Basic;
    }

    public async Task<FactionMemberBalance?> GetMemberFactionBalanceByIdAsync(ulong guildId, ulong userId, string key,
        CancellationToken ct = default)
    {
        var response = await GetAsync<FactionBalanceResponse>("faction/balance", key, ct);

        var tornProfile = await GetUserProfileByDiscordId(userId, key, ct);
        var memberBalance = response.Balance.Members.FirstOrDefault(x => x.Id == tornProfile.Id);

        return memberBalance;
    }

    public async Task<IReadOnlyList<TornItem>?> GetItemsInfoAsync(IEnumerable<int> itemIds, string key,
        CancellationToken ct = default)
    {
        var ids = itemIds.ToList();
        if (ids.Count == 0)
            return [];

        var response = await GetAsync<TornItemsResponse>($"torn/{string.Join(',', ids)}/items", key, ct);

        return response.Items;
    }
}