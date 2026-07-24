using System.Text.Json;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;
using TornBot.Bot.Shared;
using TornBot.Infrastructure.Persistence;

namespace TornBot.Bot.Features.Configurations.Retaliation;

public class NotificationChannelMenuModule(ModuleConfigRepository repository)
    : ComponentInteractionModule<ChannelMenuInteractionContext>
{
    [ComponentInteraction("retal_notification_channel")]
    public async Task SetNotificationChannel()
    {
        var config = await repository.GetRetalModuleConfigByGuildId(Context.Guild!.Id);
        if (config == null)
        {
            var msg = MessageFactory.CreateErrorMessage<InteractionMessageProperties>("Module configuration not found");
            await Context.Interaction.SendResponseAsync(InteractionCallback.Message(msg));
        }

        config!.NotificationChannelId = Context.SelectedValues.Select(x => x.Id).SingleOrDefault();
        var jsonDoc = JsonDocument.Parse(JsonSerializer.Serialize(config));
        await repository.UpdateModuleConfig(Context.Guild!.Id, Module.Retal, jsonDoc);

        await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredModifyMessage);
    }
}