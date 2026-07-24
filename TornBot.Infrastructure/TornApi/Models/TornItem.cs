using System.Text.Json.Serialization;

namespace TornBot.Bot.Infrastructure.TornApi.Models;

public class TornItem
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string? Requirement { get; set; }
    public string Image { get; set; } = "";
    public string Type { get; set; } = "";
    public string SubType { get; set; } = "";
    [JsonPropertyName("is_marked")] public bool IsMarked { get; set; }
    [JsonPropertyName("is_tradable")] public bool IsTradable { get; set; }
    [JsonPropertyName("is_found_in_city")] public bool IsFoundInCity { get; set; }
    public TornItemValue Value { get; set; } = new();

    public long Circulation { get; set; }
    // TODO: item details skipped for now
}