namespace TornBot.Bot.Infrastructure.TornApi.Models;

public class FactionCrimeReward
{
    public long Money { get; set; }
    public FactionCrimeRewardItem[] Items { get; set; } = [];
    public int Respect {get; set;}
    public int Scope { get; set; }
    public FactionCrimeRewardPayout? Payout { get; set; }
}