using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TornBot.Bot.Domain.Enums;
using TornBot.Bot.Domain.Models;
using TornBot.Bot.Infrastructure;

namespace TornBot.Bot.Shared;

public class ApiKeyService(TornbotContext context, ILogger<ApiKeyService> logger)
{
    public async Task<ApiKey?> GetApiKeyAsync(string key)
    {
        return await context.ApiKeys.FirstOrDefaultAsync(k => k.Key == key);
    }

    public async Task<string?> GetPublicApiKeyAsync()
    {
        var key = await context.ApiKeys.FirstOrDefaultAsync(k => k.AccessLevel == AccessLevel.Public);
        
        return key?.Key;
    }

    public async Task<IReadOnlyList<ApiKey>> GetAllApiKeysAsync()
    {
        return await context.ApiKeys.ToListAsync();
    }
    
    public async Task<IReadOnlyList<ApiKey>> GetApiKeysByUserIdAsync(int userId)
    {
        return await context.ApiKeys.Where(k => k.TornPlayerId == userId).ToListAsync();
    }

    public async Task<ApiKey?> GetLimitedApiKeyAsync(bool hasFactionAccess = false)
    {
        return await context.ApiKeys
            .FirstOrDefaultAsync(k => k.AccessLevel == AccessLevel.LimitedAccess && k.HasFactionAccess);
    }
}