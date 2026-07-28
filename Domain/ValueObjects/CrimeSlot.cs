using TornBot.Bot.Domain.Models;

namespace TornBot.Bot.Domain.ValueObjects;

public class CrimeSlot
{
    public required string Position { get; set; }
    public int Cpr { get; set; }
    public Player? Player { get; set; }
}