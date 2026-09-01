using TornBot.Bot.Domain.Enums;

namespace TornBot.Bot.Domain.Models;

public class FactionMember
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int Level { get; set; }
    public int DaysInFaction { get; set; }
    public bool IsRevivable { get; set; }
    public bool InOc { get; set; }
    public bool CanEarlyDischarge { get; set; }
    public ActivityStatus ActivityStatus { get; set; }
    public PlayerState CurrentState { get; set; }
}

public enum ActivityStatus
{
    Online,
    Idle,
    Offline
}