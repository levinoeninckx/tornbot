using TornBot.Domain.Enums;

namespace TornBot.Domain.Models;

public class BankingModuleConfig
{
    public HashSet<ulong> AllowedRoleIds { get; set; } = [];
    public HashSet<ulong> RestrictedChannelIds { get; set; } = [];
    public ulong? BankerRoleId { get; set; }
    public ModuleState State { get; set; }
}