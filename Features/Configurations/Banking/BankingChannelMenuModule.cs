using System.Text.Json;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;
using TornBot.Bot.Domain.Enums;
using TornBot.Bot.Infrastructure;
using TornBot.Bot.Shared;

namespace TornBot.Bot.Features.Configurations.Banking;

public class BankingChannelMenuModule(ModuleConfigRepository repository) : ComponentInteractionModule<ChannelMenuInteractionContext>
{
    [ComponentInteraction("banking_restricted_channels")]
    public async Task SetBankingChannels()
    {
        var config = await repository.GetBankingModuleConfigByGuildId(Context.Guild!.Id);
        if (config == null)
        {
            var msg = MessageFactory.CreateErrorMessage<InteractionMessageProperties>("Module configuration not found");
            await Context.Interaction.SendResponseAsync(InteractionCallback.Message(msg));
        }

        config!.RestrictedChannelIds = Context.SelectedValues.Select(x => x.Id).ToHashSet();
        var jsonDoc = JsonDocument.Parse(JsonSerializer.Serialize(config));
        await repository.UpdateModuleConfig(Context.Guild!.Id, Module.Banking, jsonDoc);
        
        await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredModifyMessage);
    }
}