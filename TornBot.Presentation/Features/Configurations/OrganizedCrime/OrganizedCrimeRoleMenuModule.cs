using System.Text.Json;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;
using TornBot.Bot.Shared;
using TornBot.Infrastructure.Persistence;

namespace TornBot.Bot.Features.Configurations.OrganizedCrime;

public class OrganizedCrimeRoleMenuModule(ModuleConfigRepository repository)
    : ComponentInteractionModule<RoleMenuInteractionContext>
{
    [ComponentInteraction("oc_allowed_roles")]
    public async Task SetAllowedRoles()
    {
        var config = await repository.GetOrganizedCrimeModuleConfigByGuildId(Context.Guild!.Id);
        if (config == null)
        {
            var msg = MessageFactory.CreateErrorMessage<InteractionMessageProperties>("Module configuration not found");
            await Context.Interaction.SendResponseAsync(InteractionCallback.Message(msg));
        }

        config!.AllowedRoleIds = Context.SelectedValues.Select(x => x.Id).ToHashSet();
        var jsonDoc = JsonDocument.Parse(JsonSerializer.Serialize(config));
        await repository.UpdateModuleConfig(Context.Guild!.Id, Module.OrganizedCrime, jsonDoc);

        await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredModifyMessage);
    }

    [ComponentInteraction("oc_notification_role")]
    public async Task SetNotificationRole()
    {
        var config = await repository.GetOrganizedCrimeModuleConfigByGuildId(Context.Guild!.Id);
        if (config == null)
        {
            var msg = MessageFactory.CreateErrorMessage<InteractionMessageProperties>("Module configuration not found");
            await Context.Interaction.SendResponseAsync(InteractionCallback.Message(msg));
        }

        config!.NotificationRoleId = Context.SelectedValues.Select(x => x.Id).SingleOrDefault();
        var jsonDoc = JsonDocument.Parse(JsonSerializer.Serialize(config));
        await repository.UpdateModuleConfig(Context.Guild!.Id, Module.OrganizedCrime, jsonDoc);

        await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredModifyMessage);
    }
}