using NetCord.Rest;
using NetCord.Services.ComponentInteractions;
using TornBot.Bot.Domain.Enums;

namespace TornBot.Bot.Features.Configurations.Retaliation;

public class NotificationStringMenuModule(ConfigurationService configurationService) : ComponentInteractionModule<StringMenuInteractionContext>
{
    [ComponentInteraction("retal_enabled")]
    public async Task SetEnabled()
    {
        var state = Enum.Parse<ModuleState>(Context.SelectedValues.Single());
        await configurationService
            .UpdateRetalConfigByGuildIdAsync(Context.Guild!.Id, config => config!.State = state);

        await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredModifyMessage);
    }
}