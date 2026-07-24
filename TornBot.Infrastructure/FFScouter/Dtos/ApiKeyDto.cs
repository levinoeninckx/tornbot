using System.Text.Json.Serialization;

namespace TornBot.Infrastructure.FFScouter.Models;

public class ApiKeyDto
{
    public required string Key { get; set; }
    [JsonPropertyName("is_registered")] public bool IsRegistered { get; set; }
    [JsonPropertyName("is_premium")] public bool IsPremium { get; set; }
}