using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TornBot.Bot.Domain.Enums;
using TornBot.Bot.Domain.Models;
using TornBot.Bot.Infrastructure;

namespace TornBot.Bot.Features.Configurations;

public class ConfigurationService(IDbContextFactory<TornbotContext> dbContextFactory, ILogger<ConfigurationService> logger)
{
    public async Task<bool> UpdateBankingConfigByGuildIdAsync(ulong guildId, Action<BankingModuleConfig> updateAction)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var faction = await dbContext.Factions.GetFactionByGuildIdAsync(guildId, includeModuleConfigs: true);
        if (faction == null)
        {
            logger.LogWarning("Faction not found for guild {GuildId}", guildId);
            return false;
        }

        var config = faction.BankingModuleConfig;
        if (config == null)
        {
            logger.LogWarning("Banking module config not found for guild {GuildId}", guildId);
            return false;
        }

        updateAction(config);

        var jsonDoc = JsonDocument.Parse(JsonSerializer.Serialize(config));
        await dbContext.Factions.UpdateModuleConfig(faction.GuildId, Module.Banking, jsonDoc);
        await dbContext.SaveChangesAsync();
        
        return true;
    }

    public async Task<bool> UpdateOrganizedCrimeConfigByGuildIdAsync(ulong guildId,
        Action<OrganizedCrimeModuleConfig> updateAction)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var faction = await dbContext.Factions.GetFactionByGuildIdAsync(guildId, includeModuleConfigs: true);
        if (faction == null)
        {
            logger.LogWarning("Faction not found for guild {GuildId}", guildId);
            return false;
        }

        var config = faction.OrganizedCrimeModuleConfig;
        if (config == null)
        {
            logger.LogWarning("Banking module config not found for guild {GuildId}", guildId);
            return false;
        }

        updateAction(config);

        var jsonDoc = JsonDocument.Parse(JsonSerializer.Serialize(config));
        await dbContext.Factions.UpdateModuleConfig(faction.GuildId, Module.OrganizedCrime, jsonDoc);
        await dbContext.SaveChangesAsync();
        
        return true;
    }

    public async Task<bool> UpdateRetalConfigByGuildIdAsync(ulong guildId, Action<RetalModuleConfig> updateConfig)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var faction = await dbContext.Factions.GetFactionByGuildIdAsync(guildId, includeModuleConfigs: true);
        if (faction == null)
        {
            logger.LogWarning("Faction not found for guild {GuildId}", guildId);
            return false;
        }

        var config = faction.RetalModuleConfig;
        if (config == null)
        {
            logger.LogWarning("Banking module config not found for guild {GuildId}", guildId);
            return false;
        }

        updateConfig(config);

        var jsonDoc = JsonDocument.Parse(JsonSerializer.Serialize(config));
        await dbContext.Factions.UpdateModuleConfig(faction.GuildId, Module.Retal, jsonDoc);
        await dbContext.SaveChangesAsync();
        
        return true;
    }
}