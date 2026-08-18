using Microsoft.Extensions.Logging;
using NetCord.Rest;

namespace TornBot.Bot.Shared;

public class NotificationService(RestClient restClient, ILogger<NotificationService> logger)
{
    private const int MaxEmbedsPerMessage = 10;

    public async Task<RestMessage> SendNotificationAsync(NotificationCommand command)
    {
        if (command.RoleId.HasValue)
            command.MessageProperties.WithContent($"<@&{command.RoleId.Value}>");

        var restMessage = await restClient.SendMessageAsync(command.ChannelId, command.MessageProperties);

        logger.LogInformation("Sent notification to channel {ChannelId} with message id {messageId}", command.ChannelId,
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
            await SendNotificationAsync(new NotificationCommand
            {
                ChannelId = channelId,
                MessageProperties = new MessageProperties { Embeds = batch },
                RoleId = roleId
            });
        }

        logger.LogInformation("Sent {EmbedCount} embeds to channel {ChannelId}", embeds.Count, channelId);
    }
}