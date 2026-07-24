using Microsoft.Extensions.Logging;
using NetCord.Rest;

namespace TornBot.Bot.Shared;

public class NotificationService(RestClient restClient, ILogger<NotificationService> logger)
{
    public async Task SendNotificationAsync(ulong channelId, MessageProperties message, ulong? roleId = null)
    {
        message.WithContent(roleId.HasValue ? $"<@&{roleId.Value}>" : null);
        await restClient.SendMessageAsync(channelId, message);

        logger.LogInformation("Sent notification to channel {ChannelId}", channelId);
    }
}