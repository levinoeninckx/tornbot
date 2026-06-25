using System.Text.Json;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;
using TornBot.Bot.Domain.Enums;
using TornBot.Bot.Infrastructure;
using TornBot.Bot.Shared;

namespace TornBot.Bot.Features.Configurations.Banking;

public class BankingStringMenuModule(ModuleConfigRepository repository) : ComponentInteractionModule<StringMenuInteractionContext>
{
    [ComponentInteraction("banking_enabled")]
    public async Task ToggleBankingAllowed()
    {
        var config = await repository.GetBankingModuleConfigByGuildId(Context.Guild!.Id);

        if (config == null)
        {
            var msg = MessageFactory.CreateErrorMessage<InteractionMessageProperties>("Module configuration not found");
            await Context.Interaction.SendResponseAsync(InteractionCallback.Message(msg));
        }

        var state = Enum.Parse<ModuleState>(Context.SelectedValues.Single());
        
        config!.State = state;
        
        var jsonDoc = JsonDocument.Parse(JsonSerializer.Serialize(config));
        if (!(await repository.UpdateModuleConfig(Context.Guild.Id, Module.Banking, jsonDoc)))
        {
            var msg = MessageFactory.CreateErrorMessage<InteractionMessageProperties>("Could not update module config");
            await Context.Interaction.SendResponseAsync(InteractionCallback.Message(msg));
        }

        await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredModifyMessage);
    }
}