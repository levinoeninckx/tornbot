using Microsoft.EntityFrameworkCore;
using TornBot.Domain.Enums;
using TornBot.Domain.Models;
using TornBot.Domain.Repositories;

namespace TornBot.Infrastructure.Persistence.Repositories;

public class ApiKeyRepository(IDbContextFactory<TornbotContext> dbContextFactory) : IApiKeyRepository
{
    public Task<ApiKey?> GetApiKeyByFactionIdAsync(int factionId, AccessLevel accessLevel,
        bool hasFactionAccess = false)
    {
        throw new NotImplementedException();
    }

    public Task AddApiKeyAsync(ApiKey apiKey)
    {
        throw new NotImplementedException();
    }

    public Task RemoveApiKeyAsync(ApiKey apiKey)
    {
        throw new NotImplementedException();
    }
}