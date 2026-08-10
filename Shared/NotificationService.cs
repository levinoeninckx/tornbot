using Microsoft.Extensions.Logging;
using NetCord.Rest;

namespace TornBot.Bot.Shared;

public class NotificationService(RestClient restClient, ILogger<NotificationService> logger)
{
    private const int MaxEmbedsPerMessage = 10;

    public async Task<RestMessage> SendNotificationAsync(ulong channelId, MessageProperties message,
        ulong? roleId = null)
    {
        if (roleId.HasValue)
            message.WithContent($"<@&{roleId.Value}>");

        var restMessage = await restClient.SendMessageAsync(channelId, message);

        logger.LogInformation("Sent notification to channel {ChannelId} with message id {messageId}", channelId,
            restMessage.Id);

        return restMessage;
    }

    /// <summary>
    /// Sends the embeds to the channel, splitting them across multiple messages to respect
    /// Discord's limit of <see cref="MaxEmbedsPerMessage"/> embeds per message.
    /// </summary>
    public async Task SendEmbedsAsync(ulong channelId, IReadOnlyList<EmbedProperties> embeds, ulong? roleId = null)
    {
        for (var i = 0; i < embeds.Count; i += MaxEmbedsPerMessage)
        {
            var batch = embeds.Skip(i).Take(MaxEmbedsPerMessage).ToList();
            await SendNotificationAsync(channelId, new MessageProperties { Embeds = batch }, roleId);
        }

        logger.LogInformation("Sent {EmbedCount} embeds to channel {ChannelId}", embeds.Count, channelId);
    }
}