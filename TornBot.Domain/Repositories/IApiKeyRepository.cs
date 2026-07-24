using TornBot.Domain.Enums;
using TornBot.Domain.Models;

namespace TornBot.Domain.Repositories;

public interface IApiKeyRepository
{
    public Task<ApiKey?> GetApiKeyByFactionIdAsync(int factionId, AccessLevel accessLevel,
        bool hasFactionAccess = false);

    public Task AddApiKeyAsync(int factionId, ApiKey apiKey);
    public Task RemoveApiKeyAsync(int factionId, ApiKey apiKey);
}