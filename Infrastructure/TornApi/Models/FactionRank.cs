namespace TornBot.Bot.Infrastructure.TornApi.Models;

public class FactionRank
{
    public int Level { get; set; }
    public string Name { get; set; } = "";
    public int Division { get; set; }
    public int Position { get; set; }
    public int Wins { get; set; }
}