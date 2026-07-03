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
        await using var context = await contextFactory.CreateDbContextAsync();
        var faction = await context.Factions
            .Include(faction => faction.ModuleConfigs)
            .SingleOrDefaultAsync(x => x.GuildId == guildId);

        var moduleConfig = faction?.ModuleConfigs.SingleOrDefault(x => x.Module == Module.Verification);

        if (moduleConfig == null && faction != null)
        {
            var newConfig = new ModuleConfig()
            {
                Module = Module.Banking,
                Config = JsonDocument.Parse(JsonSerializer.Serialize(new VerificationConfig()))
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

            return newConfig.Config.Deserialize<VerificationConfig>();
        }

        return moduleConfig?.Config.Deserialize<VerificationConfig>();
    }
    public async Task<BankingModuleConfig?> GetBankingModuleConfigByGuildId(ulong guildId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var faction = await context.Factions
            .Include(faction => faction.ModuleConfigs)
            .SingleOrDefaultAsync(x => x.GuildId == guildId);

        var moduleConfig = faction?.ModuleConfigs.SingleOrDefault(x => x.Module == Module.Banking);

        if (moduleConfig == null && faction != null)
        {
            var newConfig = new ModuleConfig()
            {
                Module = Module.Banking,
                Config = JsonDocument.Parse(JsonSerializer.Serialize(new BankingModuleConfig()))
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

            return newConfig.Config.Deserialize<BankingModuleConfig>();
        }

        return moduleConfig?.Config.Deserialize<BankingModuleConfig>();
    }
    public async Task<OrganizedCrimeModuleConfig?> GetOrganizedCrimeModuleConfigByGuildId(ulong guildId) 
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var faction = await context.Factions
            .Include(faction => faction.ModuleConfigs)
            .SingleOrDefaultAsync(x => x.GuildId == guildId);

        var moduleConfig = faction?.ModuleConfigs.SingleOrDefault(x => x.Module == Module.OrganizedCrime);

        if (moduleConfig == null && faction != null)
        {
            var newConfig = new ModuleConfig()
            {
                Module = Module.OrganizedCrime,
                Config = JsonDocument.Parse(JsonSerializer.Serialize(new OrganizedCrimeModuleConfig()))
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

            return newConfig.Config.Deserialize<OrganizedCrimeModuleConfig>();
        }

        return moduleConfig?.Config.Deserialize<OrganizedCrimeModuleConfig>();
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