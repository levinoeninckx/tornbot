using System.Text.Json;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;
using TornBot.Bot.Shared;
using TornBot.Infrastructure.Persistence;

namespace TornBot.Bot.Features.Configurations.Banking;

public class BankingRoleMenuModule(ModuleConfigRepository repository)
    : ComponentInteractionModule<RoleMenuInteractionContext>
{
    [ComponentInteraction("banker_roles")]
    public async Task SetBankerRoles()
    {
        var config = await repository.GetBankingModuleConfigByGuildId(Context.Guild!.Id);
        if (config == null)
        {
            var msg = MessageFactory.CreateErrorMessage<InteractionMessageProperties>("Module configuration not found");
            await Context.Interaction.SendResponseAsync(InteractionCallback.Message(msg));
        }

        config!.BankerRoleId = Context.SelectedValues.Single().Id;
        var jsonDoc = JsonDocument.Parse(JsonSerializer.Serialize(config));
        await repository.UpdateModuleConfig(Context.Guild!.Id, Module.Banking, jsonDoc);

        await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredModifyMessage);
    }

    [ComponentInteraction("banking_allowed_roles")]
    public async Task SetAllowedRoles()
    {
        var config = await repository.GetBankingModuleConfigByGuildId(Context.Guild!.Id);
        if (config == null)
        {
            var msg = MessageFactory.CreateErrorMessage<InteractionMessageProperties>("Module configuration not found");
            await Context.Interaction.SendResponseAsync(InteractionCallback.Message(msg));
        }

        config!.AllowedRoleIds = Context.SelectedValues.Select(x => x.Id).ToHashSet();
        var jsonDoc = JsonDocument.Parse(JsonSerializer.Serialize(config));
        await repository.UpdateModuleConfig(Context.Guild!.Id, Module.Banking, jsonDoc);

        await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredModifyMessage);
    }
}