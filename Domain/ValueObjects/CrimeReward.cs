namespace TornBot.Bot.Domain.ValueObjects;

public class CrimeReward
{
    public uint Money { get; set; }
    public uint Respect { get; set; }
    public IList<CrimeRewardItem> Items { get; set; } = [];
}

public class CrimeRewardItem
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Quantity { get; set; }
}