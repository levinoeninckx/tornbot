namespace TornBot.Bot.Domain.Models;

public class RetalModuleConfig
{
    public bool Enabled { get; set; } = false;
    public ulong? ChannelId { get; set; }
}