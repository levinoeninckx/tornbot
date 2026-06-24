namespace TornBot.Bot.Domain.Models;

public class VerificationConfig
{
    public HashSet<ulong> DefaultRoleIds { get; set; } = [];
    public HashSet<ulong> FactionRoleIds { get; set; } = [];
    public HashSet<ulong> RestrictedChannelIds { get; set; } = [];
    public ulong AutoVerificationChannelId { get; set; }
}