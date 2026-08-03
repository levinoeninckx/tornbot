using TornBot.Bot.Domain.ValueObjects;

namespace TornBot.Bot.Domain.Models;

public class Player
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public int Level { get; set; }
    public string Gender { get; set; } = "";
    public BattleStat? BattleStat { get; set; }
    public PlayerStats? PlayerStats { get; set; }
    public int? FactionId { get; set; }
    public Faction? Faction { get; set; }
}