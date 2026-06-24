using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;
using TornBot.Bot.Domain.Enums;
using TornBot.Bot.Domain.Models;
using TornBot.Bot.Infrastructure;
using TornBot.Bot.Shared;

namespace TornBot.Bot.Features.Configurations;

public class ChannelMenuModule(TornbotContext context, ILogger<ChannelMenuModule> logger) : ComponentInteractionModule<ChannelMenuInteractionContext>
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
            var msg = MessageFactory.CreateErrorMessage<InteractionMessageProperties>("register faction first with /configure faction");
            await Context.Interaction.SendFollowupMessageAsync(msg);
            await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredModifyMessage);
            return;
        }
        
        var moduleConfig = faction.ModuleConfigs.SingleOrDefault(c => c.Module == Module.Verification);
        var config = moduleConfig?.Config.Deserialize<VerificationConfig>();

        if (config == null || moduleConfig == null)
        {
            var msg = MessageFactory.CreateErrorMessage<InteractionMessageProperties>();
            await Context.Interaction.SendFollowupMessageAsync(msg);
            await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredModifyMessage);
            return;
        }
        
        config.AutoVerificationChannelId = Context.SelectedValues.Single().Id;
        moduleConfig.Config = JsonDocument.Parse(JsonSerializer.Serialize(config));

        try
        {
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Failed to save module config");
            await Context.Interaction.SendFollowupMessageAsync(MessageFactory.CreateErrorMessage<InteractionMessageProperties>("Failed to save module config"));
        }

        await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredModifyMessage);
    }
}