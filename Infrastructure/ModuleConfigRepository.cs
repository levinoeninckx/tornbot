using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TornBot.Bot.Domain.Enums;
using TornBot.Bot.Features.Banking;

namespace TornBot.Bot.Infrastructure;

public class ModuleConfigRepository(TornbotContext context)
{
    public async Task<BankingModuleConfig?> GetBankingModuleConfigByGuildId(ulong guildId)
    {
        var faction = await context.Factions
            .Include(faction => faction.ModuleConfigs)
            .SingleOrDefaultAsync(x => x.GuildId == guildId);

        var moduleConfig = faction?.ModuleConfigs.SingleOrDefault(x => x.Module == Module.Banking);

        return moduleConfig?.Config.Deserialize<BankingModuleConfig>();
    }
}