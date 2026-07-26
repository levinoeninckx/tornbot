using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TornBot.Bot.Domain.Enums;
using TornBot.Bot.Domain.Models;

namespace TornBot.Bot.Infrastructure;

public class ModuleConfigRepository(
    IDbContextFactory<TornbotContext> contextFactory,
    ILogger<ModuleConfigRepository> logger)
{
    public async Task<VerificationConfig?> GetVerificationConfigByGuildId(ulong guildId)
    {
        return await GetModuleConfigByGuildId<VerificationConfig>(guildId, Module.Verification);
    }

    public async Task<BankingModuleConfig?> GetBankingModuleConfigByGuildId(ulong guildId)
    {
        return await GetModuleConfigByGuildId<BankingModuleConfig>(guildId, Module.Banking);
    }

    public async Task<OrganizedCrimeModuleConfig?> GetOrganizedCrimeModuleConfigByGuildId(ulong guildId)
    {
        return await GetModuleConfigByGuildId<OrganizedCrimeModuleConfig>(guildId, Module.OrganizedCrime);
    }

    public async Task<RetalModuleConfig?> GetRetalModuleConfigByGuildId(ulong guildId)
    {
        return await GetModuleConfigByGuildId<RetalModuleConfig>(guildId, Module.Retal);
    }

    private async Task<T?> GetModuleConfigByGuildId<T>(ulong guildId, Module module) where T : class, new()
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var faction = await context.Factions
            .Include(f => f.ModuleConfigs)
            .SingleOrDefaultAsync(x => x.GuildId == guildId);

        if (faction == null)
            return null;

        var moduleConfig = faction.ModuleConfigs.SingleOrDefault(x => x.Module == module);
        if (moduleConfig != null)
            return moduleConfig.Config.Deserialize<T>();

        // No config exists yet for this module: lazily create and persist a default one.
        var defaultConfig = new T();
        faction.ModuleConfigs.Add(new ModuleConfig
        {
            Module = module,
            Config = JsonSerializer.SerializeToDocument(defaultConfig)
        });

        try
        {
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Failed to save default {Module} config for guild {GuildId}", module, guildId);
            return null;
        }

        return defaultConfig;
    }

    public async Task<bool> UpdateModuleConfig(ulong guildId, Module module, JsonDocument jsonConfig)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var faction = await context.Factions
            .Include(faction => faction.ModuleConfigs)
            .SingleOrDefaultAsync(x => x.GuildId == guildId);

        if (faction == null)
            return false;

        foreach (var config in faction.ModuleConfigs)
        {
            if (config.Module == module)
            {
                config.Config = jsonConfig;
            }
        }

        try
        {
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Failed to save module config");
            return false;
        }

        return true;
    }
}