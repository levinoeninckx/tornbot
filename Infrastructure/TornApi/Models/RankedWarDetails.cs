namespace TornBot.Bot.Infrastructure.TornApi.Models;

public class RankedWarResponse
{
    public int Id { get; set; }
    public string Status { get; set; } = "";

    public FactionSide Faction { get; set; } = new();
    public FactionSide Opponent { get; set; } = new();
}

public class FactionSide
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Score { get; set; }
}