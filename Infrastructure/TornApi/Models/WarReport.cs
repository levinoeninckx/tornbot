namespace TornBot.Bot.Infrastructure.TornApi.Models;

public class RankedWarReportResponse
{
    public int WarId { get; set; }
    public List<WarHit> Hits { get; set; } = new();
}

public class WarHit
{
    public int AttackerId { get; set; }
    public int DefenderId { get; set; }
    public int Respect { get; set; }
    public long Timestamp { get; set; }
}