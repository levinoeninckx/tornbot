using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;
using TornBot.Bot.Domain.Models;
using TornBot.Bot.Infrastructure;
using TornBot.Bot.Shared;

namespace TornBot.Bot.Features.Configurations;

public class RoleMenuModule(TornbotContext context) : ComponentInteractionModule<RoleMenuInteractionContext>
{
    [ComponentInteraction("default_verification_roles")]
    public async Task SetDefaultVerificationRoles()
    {
        var existingDefaultRoles = await context.AuthRoles.Where(r => r.IsDefault).ToListAsync();
        
        if (Context.Guild == null)
        {
            return;
        }
        
        var faction = await context.Factions.SingleOrDefaultAsync(f => f.GuildId == Context.Guild.Id);

        if (faction == null)
        {
            return;
        }
        
        var authRolesToAdd = Context.SelectedValues
            .Where(r => existingDefaultRoles.All(e => e.RoleId != r.Id))
            .Select(r => new AuthRole
            {
                FactionId = faction!.Id,
                RoleId = r.Id,
                IsDefault = true
            })
            .ToImmutableList();

        context.AuthRoles.AddRange(authRolesToAdd);

        foreach (var defaultRole in existingDefaultRoles)
        {
            if(Context.SelectedValues.All(v => v.Id != defaultRole.RoleId))
            {
                defaultRole.IsDefault = false;
            }
        }
        
        context.AuthRoles.UpdateRange(existingDefaultRoles);
        
        try
        {
            await context.SaveChangesAsync();
        }
        catch (Exception e)
        {
            await Context.Interaction.SendFollowupMessageAsync("Something went wrong");
        }
        
        await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredModifyMessage);
    }
}