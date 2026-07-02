using System.Text.Json.Serialization;

namespace TornBot.Bot.Infrastructure.TornApi.Models;

public class FactionCrime
{
    public int Id { get; set; }
    [JsonPropertyName("previous_crime_id")]
    public int? PreviousCrimeId { get; set; }
    public string Name { get; set; } = "";   
    public int Difficulty { get; set; }
    public string Status { get; set; } = "";  // "Recruiting","Planning","Successful","Failure","Expired"
    [JsonPropertyName("created_at")]
    public long CreatedAt { get; set; }
    [JsonPropertyName("planning_at")]
    public long? PlanningAt { get; set; }
    [JsonPropertyName("executed_at")]
    public long? ExecutedAt { get; set; }
    [JsonPropertyName("ready_at")]
    public long? ReadyAt { get; set; }
    [JsonPropertyName("expired_at")]
    public long ExpiredAt { get; set; }
    public FactionCrimeSlot[] Slots { get; set; } = [];
    public FactionCrimeReward Rewards { get; set; }
}