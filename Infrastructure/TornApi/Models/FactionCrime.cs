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
    public int CreatedAt { get; set; }

    [JsonPropertyName("planning_at")]
    public int? PlanningAt { get; set; }

    [JsonPropertyName("executed_at")]
    public int? ExecutedAt { get; set; }

    [JsonPropertyName("ready_at")]
    public int? ReadyAt { get; set; }

    [JsonPropertyName("expired_at")]
    public int ExpiredAt { get; set; }

    public FactionCrimeSlot[] Slots { get; set; } = [];
    public FactionCrimeReward Rewards { get; set; }
}