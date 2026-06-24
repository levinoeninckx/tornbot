using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;
using TornBot.Bot.Domain.Enums;
using TornBot.Bot.Domain.Models;
using TornBot.Bot.Infrastructure;

namespace TornBot.Bot.Features.Configurations;

public class ChannelMenuModule(TornbotContext context) : ComponentInteractionModule<ChannelMenuInteractionContext>
{
    [ComponentInteraction("auto_verification_channel")]
    public async Task SetAutoVerificationChannel()
    {
        if (Context.Guild == null)
        {
            return;
        }
        
        var faction = await context.Factions
            .Include(f => f.ModuleConfigs)
            .SingleOrDefaultAsync(f => f.GuildId == Context.Guild.Id);

        if (faction == null)
        {
            // TODO: send message to register faction
            return;
        }
        
        var moduleConfig = faction.ModuleConfigs.SingleOrDefault(c => c.Module == Module.Verification);
        var config = moduleConfig?.Config.Deserialize<VerificationConfig>();

        if (config == null || moduleConfig == null)
        {
            return;
        }
        
        config.AutoVerificationChannelId = Context.SelectedValues.Single().Id;
        moduleConfig.Config = JsonDocument.Parse(JsonSerializer.Serialize(config));
        
        await context.SaveChangesAsync();

        await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredModifyMessage);
    }
}