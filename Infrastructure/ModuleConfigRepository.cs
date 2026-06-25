using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TornBot.Bot.Domain.Enums;
using TornBot.Bot.Domain.Models;
using TornBot.Bot.Features.Banking;

namespace TornBot.Bot.Infrastructure;

public class ModuleConfigRepository(TornbotContext context, ILogger<ModuleConfigRepository> logger)
{
    public async Task<BankingModuleConfig?> GetBankingModuleConfigByGuildId(ulong guildId)
    {
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

    public async Task<bool> UpdateModuleConfig(ulong guildId, Module module, JsonDocument jsonConfig)
    {
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