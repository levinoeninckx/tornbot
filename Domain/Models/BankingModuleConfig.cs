using TornBot.Bot.Domain.Enums;

namespace TornBot.Bot.Domain.Models;

public class BankingModuleConfig
{
    public HashSet<ulong> AllowedRoleIds { get; set; } = [];
    public HashSet<ulong> RestrictedChannelIds { get; set; } = [];
    public ulong? BankerRoleId { get; set; }
    public ModuleState State { get; set; }
    public bool AllowDm { get; set; }
}