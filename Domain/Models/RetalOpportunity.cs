namespace TornBot.Bot.Domain.Models;

public class RetalOpportunity
{
    public long Id { get; set; }
    public required ulong AttackId { get; set; }
    public required long TargetPlayerId { get; set; }
    public required ulong MessageId { get; set; }
    public DateTime Timestamp { get; private set; } = DateTime.UtcNow;
}