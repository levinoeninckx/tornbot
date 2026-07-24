using System.Text.Json.Serialization;

namespace FactionBot.Infrastructure.TornApi.Models;

public class Faction
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string Tag { get; set; } = "";
    [JsonPropertyName("tag_image")] public string TagImage { get; set; } = "";
    public string Position { get; set; } = "";
    [JsonPropertyName("days_in_faction")] public int DaysInFaction { get; set; }
}