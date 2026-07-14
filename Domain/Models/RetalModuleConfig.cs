using TornBot.Bot.Domain.Enums;

namespace TornBot.Bot.Domain.Models;

public class RetalModuleConfig
{
    public ModuleState State { get; set; } = ModuleState.Disabled;
    public ulong? NotificationChannelId { get; set; }
    public ulong? NotificationRoleId { get; set; }
}