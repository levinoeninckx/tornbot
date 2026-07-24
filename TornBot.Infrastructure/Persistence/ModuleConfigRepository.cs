using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TornBot.Domain.Enums;
using TornBot.Domain.Models;
using TornBot.Domain.Repositories;
using TornBot.Infrastructure.Persistence.Entities;

namespace TornBot.Infrastructure.Persistence;

public class ModuleConfigRepository(
    IDbContextFactory<TornbotContext> contextFactory,
    ILogger<ModuleConfigRepository> logger) : IModuleConfigRepository
{
    public async Task<VerificationConfig?> GetVerificationConfigByFactionIdAsync(int factionId)
    {
        return await GetModuleConfigByFactionId<VerificationConfig>(factionId, ModuleType.Verification);
    }

    public async Task<BankingModuleConfig?> GetBankingModuleConfigByFactionIdAsync(int factionId)
    {
        return await GetModuleConfigByFactionId<BankingModuleConfig>(factionId, ModuleType.Banking);
    }

    public async Task<OrganizedCrimeModuleConfig?> GetOrganizedCrimeModuleConfigByFactionIdAsync(int factionId)
    {
        return await GetModuleConfigByFactionId<OrganizedCrimeModuleConfig>(factionId, ModuleType.OrganizedCrime);
    }

    public async Task<RetalModuleConfig?> GetRetalModuleConfigByFactionIdAsync(int factionId)
    {
        return await GetModuleConfigByFactionId<RetalModuleConfig>(factionId, ModuleType.Retal);
    }

    private async Task<T?> GetModuleConfigByFactionId<T>(int factionId, ModuleType module) where T : class
    {
        // TODO: fix this class and method
        await using var context = await contextFactory.CreateDbContextAsync();
        var faction = await context.Factions
            .Include(faction => faction.ModuleConfigs)
            .SingleOrDefaultAsync(x => x.Id == factionId);

        var moduleConfig = faction?.ModuleConfigs.SingleOrDefault(x => x.ModuleType == module);

        if (moduleConfig == null && faction != null)
        {
            var newConfig = new ModuleConfigEntity
            {
                ModuleType = module,
                Config = JsonDocument.Parse(JsonSerializer.Serialize(Activator.CreateInstance<T>()))
            };
            faction.ModuleConfigs.Add(newConfig);

            try
            {
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Failed to save module config");
                return null;
            }

            return newConfig.Config.Deserialize<T>();
        }

        return moduleConfig?.Config.Deserialize<T>();
    }

    public async Task<bool> UpdateModuleConfig(ulong guildId, ModuleType module, JsonDocument jsonConfig)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var faction = await context.Factions
            .Include(faction => faction.ModuleConfigs)
            .SingleOrDefaultAsync(x => x.GuildId == guildId);

        if (faction == null)
            return false;

        foreach (var config in faction.ModuleConfigs)
        {
            if (config.ModuleType == module)
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