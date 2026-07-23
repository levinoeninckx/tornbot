using System.Text.Json.Serialization;
using TornBot.Bot.Infrastructure.JsonConverters;

namespace TornBot.Bot.Infrastructure.TornApi.Models;

public class FactionCrime
{
    public int Id { get; set; }

    [JsonPropertyName("previous_crime_id")]
    public int? PreviousCrimeId { get; set; }

    public string Name { get; set; } = "";
    public int Difficulty { get; set; }
    public string Status { get; set; } = "";

    [JsonPropertyName("created_at")]
    [JsonConverter(typeof(UnixTimestampConverter))]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("planning_at")]
    [JsonConverter(typeof(UnixTimestampConverter))]
    public DateTime? PlanningAt { get; set; }

    [JsonPropertyName("executed_at")]
    [JsonConverter(typeof(UnixTimestampConverter))]
    public DateTime? ExecutedAt { get; set; }

    [JsonPropertyName("ready_at")]
    [JsonConverter(typeof(UnixTimestampConverter))]
    public DateTime? ReadyAt { get; set; }

    [JsonPropertyName("expired_at")]
    [JsonConverter(typeof(UnixTimestampConverter))]
    public DateTime ExpiredAt { get; set; }

    public FactionCrimeSlot[] Slots { get; set; } = [];
    public FactionCrimeReward Rewards { get; set; }
}