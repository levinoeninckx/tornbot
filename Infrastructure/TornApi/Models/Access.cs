namespace TornBot.Bot.Infrastructure.TornApi.Models;

public class Access
{
    public int Level { get; set; }
    public string Type { get; set; }
    public bool Faction { get; set; }
    public bool Company { get; set; }
    
    // TODO: add log property for custom permissions
}