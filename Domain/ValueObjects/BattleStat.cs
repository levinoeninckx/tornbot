namespace TornBot.Bot.Domain.ValueObjects;

public class BattleStat
{
    public ulong Estimate { get; set; }
    public BattleStatDetails? Details { get; set; }
}