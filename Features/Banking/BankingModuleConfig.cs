namespace TornBot.Bot.Features.Banking;

public class BankingModuleConfig
{
    public HashSet<ulong> AllowedRoleIds { get; set; } = [];
    public HashSet<ulong> RestrictedChannelIds { get; set; } = [];
    public ulong? BankerRoleId { get; set; }
    public bool AllowBanking { get; set; }
    public bool AllowDm { get; set; } = false;
}