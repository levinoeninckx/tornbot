using System.Text.Json;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;
using TornBot.Bot.Domain.Enums;
using TornBot.Bot.Infrastructure;
using TornBot.Bot.Shared;

namespace TornBot.Bot.Features.Configurations.Retaliation;

public class NotificationStringMenuModule(ModuleConfigRepository repository) : ComponentInteractionModule<StringMenuInteractionContext>
{
    [ComponentInteraction("retal_enabled")]
    public async Task SetEnabled()
    {
        var config = await repository.GetRetalModuleConfigByGuildId(Context.Guild!.Id);

        if (config == null)
        {
            var msg = MessageFactory.CreateErrorMessage<InteractionMessageProperties>("Module configuration not found");
            await Context.Interaction.SendResponseAsync(InteractionCallback.Message(msg));
        }

        var state = Enum.Parse<ModuleState>(Context.SelectedValues.Single());
        
        config!.State = state;
        
        var jsonDoc = JsonDocument.Parse(JsonSerializer.Serialize(config));
        if (!(await repository.UpdateModuleConfig(Context.Guild.Id, Module.Retal, jsonDoc)))
        {
            var msg = MessageFactory.CreateErrorMessage<InteractionMessageProperties>("Could not update module config");
            await Context.Interaction.SendResponseAsync(InteractionCallback.Message(msg));
        }

        await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredModifyMessage);
    }
}