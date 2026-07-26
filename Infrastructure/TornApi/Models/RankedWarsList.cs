namespace TornBot.Bot.Infrastructure.TornApi.Models;

public class FactionRankedWarsResponse
{
    public List<RankedWarSummary> RankedWars { get; set; } = new();
}

public class RankedWarSummary
{
    public int Id { get; set; }
    public int Start { get; set; }
    public int? End { get; set; }
    public int Target { get; set; }
    public int? Winner { get; set; }
    public List<TornFaction> Factions { get; set; } = [];
}