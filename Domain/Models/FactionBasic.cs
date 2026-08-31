namespace TornBot.Bot.Domain.Models;

public class FactionBasic
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public uint Respect { get; set; }
    public string Rank { get; set; } = "";
    public int MemberCount { get; set; }
}