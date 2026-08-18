using NetCord.Rest;

namespace TornBot.Bot.Shared;

public class NotificationCommand
{
    public required ulong ChannelId { get; set; }
    public ulong? RoleId { get; set; }
    public required MessageProperties MessageProperties { get; set; }
}