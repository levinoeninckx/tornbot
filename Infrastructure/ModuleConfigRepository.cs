using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TornBot.Bot.Domain.Enums;
using TornBot.Bot.Domain.Models;
using TornBot.Bot.Features.OrganizedCrime;

namespace TornBot.Bot.Infrastructure;

public class ModuleConfigRepository(IDbContextFactory<TornbotContext> contextFactory, ILogger<ModuleConfigRepository> logger)
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
    
    private async Task<T?> GetModuleConfigByGuildId<T>(ulong guildId, Module module) where T : class
    {
        // TODO: fix this class and method
        await using var context = await contextFactory.CreateDbContextAsync();
        var faction = await context.Factions
            .Include(faction => faction.ModuleConfigs)
            .SingleOrDefaultAsync(x => x.GuildId == guildId);

        var moduleConfig = faction?.ModuleConfigs.SingleOrDefault(x => x.Module == module);

        if (moduleConfig == null && faction != null)
        {
            var newConfig = new ModuleConfig()
            {
                Module = module,
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
    
    public async Task<bool> UpdateModuleConfig(ulong guildId, Module module, JsonDocument jsonConfig)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var faction = await context.Factions
            .Include(faction => faction.ModuleConfigs)
            .SingleOrDefaultAsync(x => x.GuildId == guildId);
        
        if(faction == null)
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