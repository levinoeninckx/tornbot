using TornBot.Bot.Domain.Enums;

namespace TornBot.Bot.Domain.Models;

public class Attack
{
    public required ulong Id { get; set; }
    public int? AttackerId { get; set; }
    public int? AttackerFactionId { get; set; }
    public Player? Attacker { get; set; }
    public required int DefenderId { get; set; }
    public int? DefenderFactionId { get; set; }
    public Player? Defender { get; set; }
    public required AttackResult Result { get; set; }
    public required DateTime Timestamp { get; set; }

    public bool CanBeRetaliated()
    {
        if (Attacker is null)
            return false;

        if (AttackerFactionId == DefenderFactionId)
            return false;

        if (Attacker.State is PlayerState.Abroad or PlayerState.Fallen or PlayerState.Federal or PlayerState.Traveling)
            return false;

        if (DateTime.UtcNow - Timestamp > TimeSpan.FromMinutes(5))
            return false;

        return true;
    }
}