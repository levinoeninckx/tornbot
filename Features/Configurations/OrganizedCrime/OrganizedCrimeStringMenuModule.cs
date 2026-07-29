using NetCord.Rest;
using NetCord.Services.ComponentInteractions;
using TornBot.Bot.Domain.Enums;

namespace TornBot.Bot.Features.Configurations.OrganizedCrime;

public class OrganizedCrimeStringMenuModule(ConfigurationService configurationService) : ComponentInteractionModule<StringMenuInteractionContext>
{
    [ComponentInteraction("oc_enabled")]
    public async Task SetEnabled()
    {
        var state = Enum.Parse<ModuleState>(Context.SelectedValues.Single());
        
        await configurationService
            .UpdateOrganizedCrimeConfigByGuildIdAsync(Context.Guild!.Id, config => config!.State = state);
        
        await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredModifyMessage);
    }

    [ComponentInteraction("oc_notifications_enabled")]
    public async Task SetNotificationsEnabled()
    {
        var state = Enum.Parse<ModuleState>(Context.SelectedValues.Single());
        
        await configurationService
            .UpdateOrganizedCrimeConfigByGuildIdAsync(Context.Guild!.Id, config => config.NotificationState = state);
        
        await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredModifyMessage);
    }
}
