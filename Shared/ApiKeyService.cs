using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TornBot.Bot.Domain.Enums;
using TornBot.Bot.Domain.Models;
using TornBot.Bot.Infrastructure;

namespace TornBot.Bot.Shared;

public class ApiKeyService(IDbContextFactory<TornbotContext> contextFactory, ILogger<ApiKeyService> logger)
{
    public async Task<ApiKey?> GetApiKeyAsync(string key)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.ApiKeys.FirstOrDefaultAsync(k => k.Key == key);
    }

    public async Task<string?> GetPublicApiKeyAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var key = await context.ApiKeys.FirstOrDefaultAsync(k => k.AccessLevel == AccessLevel.Public);

        return key?.Key;
    }

    public async Task<IReadOnlyList<ApiKey>> GetAllApiKeysAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.ApiKeys.ToListAsync();
    }

    public async Task<IReadOnlyList<ApiKey>> GetApiKeysByUserIdAsync(int userId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.ApiKeys.Where(k => k.TornPlayerId == userId).ToListAsync();
    }

    public async Task<ApiKey?> GetLimitedApiKeyAsync(int factionId, bool hasFactionAccess = false)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        if (hasFactionAccess)
        {
            var faction = await context.Factions
                .Include(f => f.ApiKeys)
                .SingleOrDefaultAsync(f => f.FactionId == factionId);

            return faction?.GetApiKey(AccessLevel.LimitedAccess, requireFactionAccess: true);
        }

        return await context.ApiKeys
            .FirstOrDefaultAsync(k => k.AccessLevel == AccessLevel.LimitedAccess);
    }

    public async Task<ApiKey?> GetMinimalApiKeyAsync(ulong guildId, bool hasFactionAccess = false)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var faction = await context.Factions
            .AsNoTracking()
            .Include(f => f.ApiKeys)
            .SingleOrDefaultAsync(f => f.GuildId == guildId);

        return faction?.GetApiKey(AccessLevel.Minimal, hasFactionAccess);
    }

    public async Task<ApiKey?> GetFfScouterApiKeyAsync(int factionId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var faction = await context.Factions
            .Where(f => f.FactionId == factionId)
            .Include(f => f.ApiKeys)
            .SingleOrDefaultAsync();

        if (faction is null)
        {
            logger.LogWarning("No faction found for faction {factionId}", factionId);
            return null;
        }

        return faction.GetApiKey(AccessLevel.FfScouter);
    }

    public async Task<ApiKey?> GetTornStatsApiKeyAsync(int factionId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var faction = await context.Factions
            .Where(f => f.FactionId == factionId)
            .Include(f => f.ApiKeys)
            .SingleOrDefaultAsync();

        if (faction == null)
        {
            logger.LogWarning("No faction found for faction {factionId}", factionId);
            return null;
        }

        return faction.GetApiKey(AccessLevel.TornStats);
    }
}