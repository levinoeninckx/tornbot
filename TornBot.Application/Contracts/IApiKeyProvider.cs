using TornBot.Domain.Models;

namespace TornBot.Application.Contracts;

public interface IApiKeyProvider
{
    public Task<bool> ValidateApiKeyAsync(string apiKey, CancellationToken ct = default);
    public Task<ApiKey?> GetApiKeyByKeyAsync(string key, CancellationToken ct = default);
}