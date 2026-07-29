using NetCord.Rest;
using NetCord.Services.ComponentInteractions;

namespace TornBot.Bot.Features.Configurations.Retaliation;

public class NotificationRoleMenuModule(ConfigurationService configurationService) : ComponentInteractionModule<RoleMenuInteractionContext>
{
    [ComponentInteraction("retal_notification_role")]
    public async Task SetNotificationRole()
    {
        var notificationRoleId = Context.SelectedValues.Single().Id;
        await configurationService
            .UpdateRetalConfigByGuildIdAsync(Context.Guild!.Id, config => config!.NotificationRoleId = notificationRoleId);
        
        await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredModifyMessage);
    }
}
