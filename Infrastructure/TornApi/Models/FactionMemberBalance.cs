namespace TornBot.Bot.Infrastructure.TornApi.Models;

public class FactionMemberBalance
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public long Money { get; set; }
    public int Points { get; set; }
}