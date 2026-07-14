using System.Text.Json.Serialization;

namespace TornBot.Bot.Infrastructure.FFScouter.Models;

public class Spy
{
    public ulong Strength { get; set; } 
    public ulong Speed { get; set; }
    public ulong Defense { get; set; }
    public ulong Dexterity { get; set; }
    public ulong Total { get; set; }
    [JsonPropertyName("last_updated")]
    public DateTime LastUpdated { get; set; }
    public string Source { get; set; } = "";
    [JsonPropertyName("source_faction_id")]
    public int SourceFactionId { get; set; }
}