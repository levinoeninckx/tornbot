using TornBot.Application.Contracts;
using TornBot.Domain.Enums;
using TornBot.Domain.Models;
using TornBot.Domain.Repositories;

namespace TornBot.Application.Services;

public class ApiKeyService(IApiKeyRepository repository) : IApiKeyService
{
    public Task<ApiKey?> GetPublicKeyByFactionIdAsync(int factionId)
    {
        return repository.GetApiKeyByFactionIdAsync(factionId, AccessLevel.Public);
    }

    public Task<bool> AddApiKeyAsync(int factionId, ApiKey apiKey)
    {
        repository.AddApiKeyAsync(apiKey);
        return Task.FromResult(true);
    }

    public Task<bool> RemoveApiKeyAsync(int factionId, ApiKey apiKey)
    {
        repository.RemoveApiKeyAsync(apiKey);
    }
}