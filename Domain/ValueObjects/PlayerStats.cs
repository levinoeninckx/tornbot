namespace TornBot.Bot.Domain.ValueObjects;

public class PlayerStats
{
    public int XanaxTaken { get; set; }
    public int RefillsUsed { get; set; }
    public int StatEnhancersUsed { get; set; }
    public int MeritsBought { get; set; }
    public int AttacksWon { get; set; }
    public int DefendsWon { get; set; }
    public ulong Networth { get; set; }
}