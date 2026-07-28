using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;
using TornBot.Bot.Infrastructure;
using TornBot.Bot.Shared;

namespace TornBot.Bot.Features.Configurations.Retaliation;

public class NotificationChannelMenuModule(IDbContextFactory<TornbotContext> dbContextFactory)
    : ComponentInteractionModule<ChannelMenuInteractionContext>
{
    [ComponentInteraction("retal_notification_channel")]
    public async Task SetNotificationChannel()
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var faction = await dbContext.Factions.GetFactionByGuildIdAsync(Context.Guild!.Id, includeModuleConfigs: true);
        if (faction == null)
        {
            await Context.Interaction.SendResponseAsync(InteractionCallback.Message(
                MessageFactory.CreateErrorMessage<InteractionMessageProperties>("Faction is not registered")));
            return;
        }

        var config = faction.RetalModuleConfig;

        config!.NotificationChannelId = Context.SelectedValues.Select(x => x.Id).SingleOrDefault();
        var jsonDoc = JsonDocument.Parse(JsonSerializer.Serialize(config));


        await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredModifyMessage);
    }
}