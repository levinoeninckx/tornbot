using TornBot.Bot.Domain.Enums;

namespace TornBot.Bot.Domain.Models;

public class VerificationModuleConfig
{
    public ModuleState Enabled { get; set; } = ModuleState.Enabled;
    public HashSet<ulong> DefaultRoleIds { get; set; } = [];
    public HashSet<ulong> FactionRoleIds { get; set; } = [];
    public HashSet<ulong> NonFactionRoleIds { get; set; } = [];
    public HashSet<ulong> RestrictedChannelIds { get; set; } = [];
    public HashSet<ulong> AllowedRoleIds { get; set; } = [];
    public ulong AutoVerificationChannelId { get; set; }
}