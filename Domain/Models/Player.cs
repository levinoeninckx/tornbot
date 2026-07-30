using TornBot.Bot.Features.Retaliation.Models;

namespace TornBot.Bot.Domain.Models;

public class Player
{
    public int Id { get; set; }
    public ulong DiscordId { get; set; }
    public string Username { get; set; } = "";
    public int Level { get; set; }
    public BattleStat? BattleStat { get; set; }
}