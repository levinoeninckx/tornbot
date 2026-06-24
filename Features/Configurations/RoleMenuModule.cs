using System.Collections.Immutable;
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

public class RoleMenuModule(TornbotContext context, Logger<RoleMenuModule> logger) : ComponentInteractionModule<RoleMenuInteractionContext>
{
    [ComponentInteraction("default_verification_roles")]
    public async Task SetDefaultVerificationRoles()
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
        
        config.DefaultRoleIds = [.. Context.SelectedValues.Select(r => r.Id)];
        moduleConfig.Config = JsonDocument.Parse(JsonSerializer.Serialize(config));

        try
        {
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Failed to save default verification roles");
            var msg = MessageFactory.CreateErrorMessage<InteractionMessageProperties>("Failed to save default verification roles");
            await Context.Interaction.SendFollowupMessageAsync(msg);
            await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredModifyMessage);
            return;
        }

        await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredModifyMessage);
    }
}