using TornBot.Bot.Domain.Enums;

namespace TornBot.Bot.Domain.Models;

public class OrganizedCrimeModuleConfig
{
    public ModuleState State { get; set; } = ModuleState.Enabled;
    public ModuleState NotificationState { get; set; } = ModuleState.Disabled;
    public ulong? NotificationChannelId { get; set; }
    public ulong? NotificationRoleId { get; set; }
    public HashSet<ulong> AllowedRoleIds { get; set; } = [];
    public HashSet<ulong> RestrictedChannelIds { get; set; } = [];
    public int MinimalLevelRequiredForNotification { get; set; } = 1;
}