using TornBot.Domain.Models;

namespace TornBot.Application.Contracts;

public interface IApiKeyService
{
    public Task<ApiKey?> GetPublicKeyByFactionIdAsync(int factionId);
    public Task<bool> AddApiKeyAsync(int factionId, ApiKey apiKey);
    public Task<bool> RemoveApiKeyAsync(int factionId, ApiKey apiKey);
}