using TornBot.Domain.Enums;

namespace TornBot.Domain.Models;

public class Attack
{
    public int Id { get; set; }
    public AttackResult Result { get; set; }
    public Player? Attacker { get; set; }
    public required Player Defender { get; set; }
    public DateTime Started { get; set; }
    public DateTime Ended { get; set; }
}