using System.Text.Json.Serialization;

namespace TornBot.Bot.Infrastructure.TornApi.Models;

public class FactionCrimeUser
{
    public int Id { get; set; }
    public string? Outcome { get; set; }
    [JsonPropertyName("outcome_duration")]
    public int? OutcomeDuration { get; set; }
    [JsonPropertyName("item_outcome")]
    public FactionCrimeItemOutcome? ItemOutcome { get; set; }
    [JsonPropertyName("joined_at")]
    public long JoinedAt { get; set; }
    public float Progress { get; set; }
    
}