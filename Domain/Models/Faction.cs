namespace TornBot.Bot.Domain.Models;

public class Faction
{
    public int Id { get; set; }
    public int FactionId { get; set; }
    public ulong GuildId { get; set; }
    public DateTime CreatedAt { get; set; }
}