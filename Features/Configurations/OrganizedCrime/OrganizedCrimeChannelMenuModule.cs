using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;
using TornBot.Bot.Domain.Enums;
using TornBot.Bot.Infrastructure;
using TornBot.Bot.Shared;

namespace TornBot.Bot.Features.Configurations.OrganizedCrime;

public class OrganizedCrimeChannelMenuModule(ConfigurationService configurationService) : ComponentInteractionModule<ChannelMenuInteractionContext>
{
    [ComponentInteraction("oc_restricted_channels")]
    public async Task SetRestrictedChannels()
    {
        var restrictedChannelIds = Context.SelectedValues.Select(x => x.Id).ToHashSet();
        await configurationService.UpdateOrganizedCrimeConfigByGuildIdAsync(Context.Guild!.Id,
            config => config!.RestrictedChannelIds = restrictedChannelIds);
        
        await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredModifyMessage);
    }

    [ComponentInteraction("oc_notification_channel")]
    public async Task SetNotificationChannel()
    {
        var notificationChannelId = Context.SelectedValues.Select(x => x.Id).SingleOrDefault();
        
        await configurationService
            .UpdateOrganizedCrimeConfigByGuildIdAsync(Context.Guild!.Id,
                config => config!.NotificationChannelId = notificationChannelId);
        
        await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredModifyMessage);
    }
}