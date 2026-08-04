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

    public async Task<ApiKey?> GetLimitedApiKeyAsync(ulong guildId, bool hasFactionAccess = false)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        if (hasFactionAccess)
        {
            return await context.Factions
                .Include(f => f.ApiKeys)
                .Where(f => f.GuildId == guildId)
                .SelectMany(f => f.ApiKeys)
                .FirstOrDefaultAsync(k => k.AccessLevel == AccessLevel.LimitedAccess && k.HasFactionAccess);
        }

        return await context.ApiKeys
            .FirstOrDefaultAsync(k => k.AccessLevel == AccessLevel.LimitedAccess);
    }

    public async Task<ApiKey?> GetMinimalApiKeyAsync(ulong guildId, bool hasFactionAccess = false)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var queryable = context.Factions
            .AsNoTracking()
            .Include(f => f.ApiKeys)
            .Where(f => f.GuildId == guildId)
            .SelectMany(f => f.ApiKeys);

        if (hasFactionAccess)
        {
            return await queryable.FirstOrDefaultAsync(k => k.AccessLevel == AccessLevel.Minimal && k.HasFactionAccess);
        }

        return await queryable
            .FirstOrDefaultAsync(k => k.AccessLevel == AccessLevel.Minimal);
    }

    public async Task<ApiKey?> GetFfScouterApiKeyAsync(ulong guildId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var faction = await context.Factions
            .Where(f => f.GuildId == guildId)
            .Include(f => f.ApiKeys)
            .SingleOrDefaultAsync();

        if (faction is null)
        {
            logger.LogWarning("No faction found for guild {guildId}", guildId);
            return null;
        }

        return faction.ApiKeys.FirstOrDefault(k => k.AccessLevel == AccessLevel.FfScouter);
    }

    public async Task<ApiKey?> GetTornStatsApiKeyAsync(ulong guildId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var faction = await context.Factions
            .Where(f => f.GuildId == guildId)
            .Include(f => f.ApiKeys)
            .SingleOrDefaultAsync();

        if (faction == null)
        {
            logger.LogWarning("No faction found for guild {guildId}", guildId);
            return null;
        }

        return faction.ApiKeys.FirstOrDefault(k => k.AccessLevel == AccessLevel.TornStats);
    }
}