using TornBot.Domain.Enums;

namespace TornBot.Domain.Models;

public class RetalModuleConfig
{
    public ModuleState State { get; set; } = ModuleState.Disabled;
    public ulong? NotificationChannelId { get; set; }
    public ulong? NotificationRoleId { get; set; }
}