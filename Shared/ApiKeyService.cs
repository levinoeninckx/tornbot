using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TornBot.Bot.Domain.Enums;
using TornBot.Bot.Domain.Models;
using TornBot.Bot.Infrastructure;

namespace TornBot.Bot.Shared;

public class ApiKeyService(TornbotContext context, ILogger<ApiKeyService> logger)
{
    public async Task<bool> AddKeyAsync(ApiKey key)
    {
        try
        {
            context.ApiKeys.Add(key);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add API key");
            return false;
        }

        return true;
    }
    
    public async Task<bool> RemoveKeyAsync(ApiKey key)
    {
        try
        {
            context.ApiKeys.Remove(key);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to remove API key");
            return false;
        }
        
        return true;
    }
    
    public async Task<ApiKey?> GetApiKeyAsync(string key)
    {
        return await context.ApiKeys.FirstOrDefaultAsync(k => k.Key == key);
    }

    public async Task<ApiKey?> GetPublicApiKeyAsync()
    {
        var key = await context.ApiKeys.FirstOrDefaultAsync(k => k.AccessLevel == AccessLevel.Public);
        return key;
    }

    public async Task<IReadOnlyList<ApiKey>> GetAllApiKeysAsync()
    {
        return await context.ApiKeys.ToListAsync();
    }
    
    public async Task<IReadOnlyList<ApiKey>> GetApiKeysByUserIdAsync(int userId)
    {
        return await context.ApiKeys.Where(k => k.TornPlayerId == userId).ToListAsync();
    }
}