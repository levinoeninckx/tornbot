using System.Text.Json.Serialization;

namespace TornBot.Bot.Infrastructure.FFScouter.Models;

public class ApiKey
{
    public required string Key { get; set; }
    [JsonPropertyName("is_registered")]
    public bool IsRegistered { get; set; }
    [JsonPropertyName("is_premium")]
    public bool IsPremium { get; set; }
}