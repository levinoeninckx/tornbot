using Microsoft.Extensions.Logging;
using TornBot.Application.Contracts;
using TornBot.Domain.Enums;
using TornBot.Domain.Models;
using TornBot.Infrastructure.TornApi;

namespace TornBot.Infrastructure.Services;

public class ApiKeyProvider(TornApiClient tornClient, ILogger<ApiKeyProvider> logger) : IApiKeyProvider
{
    public async Task<bool> ValidateApiKeyAsync(string apiKey, CancellationToken ct = default)
    {
        var key = await tornClient.GetKeyInfoAsync(apiKey, ct);
        if (key == null)
        {
            logger.LogWarning("Invalid API key");
            return false;
        }

        return true;
    }

    public async Task<ApiKey?> GetApiKeyByKeyAsync(string key, CancellationToken ct = default)
    {
        var keyInfo = await tornClient.GetKeyInfoAsync(key, ct);
        if (keyInfo == null)
        {
            logger.LogWarning("Invalid API key");
            return null;
        }

        var apiKey = new ApiKey(keyInfo.User.Id, key, (AccessLevel)keyInfo.Access.Level);

        return apiKey;
    }
}