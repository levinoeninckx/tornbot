namespace TornBot.Bot.Domain.Models;

public class AuthRole
{
    public int Id { get; set; }
    public int FactionId { get; set; }
    public Faction? Faction { get; set; }
    public ulong RoleId { get; set; }
    public bool IsDefault { get; set; }
}