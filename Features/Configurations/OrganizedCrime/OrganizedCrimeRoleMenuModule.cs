using System.Text.Json;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;
using TornBot.Bot.Domain.Enums;
using TornBot.Bot.Infrastructure;
using TornBot.Bot.Shared;

namespace TornBot.Bot.Features.Configurations.OrganizedCrime;

public class OrganizedCrimeRoleMenuModule(ConfigurationService configurationService) : ComponentInteractionModule<RoleMenuInteractionContext>
{
    [ComponentInteraction("oc_allowed_roles")]
    public async Task SetAllowedRoles()
    {
        var allowedRoleIds = Context.SelectedValues.Select(x => x.Id).ToHashSet();
        await configurationService.UpdateOrganizedCrimeConfigByGuildIdAsync(Context.Guild!.Id, config => config!.AllowedRoleIds = allowedRoleIds);
        
        await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredModifyMessage);
    }

    [ComponentInteraction("oc_notification_role")]
    public async Task SetNotificationRole()
    {
        var notificationRoleId = Context.SelectedValues.Select(x => x.Id).SingleOrDefault();
        await configurationService.UpdateOrganizedCrimeConfigByGuildIdAsync(Context.Guild!.Id, config => config!.NotificationRoleId = notificationRoleId);
        
        await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredModifyMessage);
    }
}