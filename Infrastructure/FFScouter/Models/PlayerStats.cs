using System.Text.Json.Serialization;
using TornBot.Bot.Infrastructure.JsonConverters;

namespace TornBot.Bot.Infrastructure.FFScouter.Models;

public class PlayerStats
{
    [JsonPropertyName("player_id")]
    public int PlayerId { get; set; }
    [JsonPropertyName("fair_fight")]
    public decimal? FairFight { get; set; }
    [JsonPropertyName("bs_estimate")]
    public ulong? BsEstimate { get; set; }
    [JsonPropertyName("bs_estimate_human")]
    public string? BsEstimateHuman { get; set; } = "";
    [JsonPropertyName("bss_public")]
    public int? BssPublic { get; set; }
    [JsonPropertyName("last_updated")]
    [JsonConverter(typeof(UnixTimestampConverter))]
    public DateTime? LastUpdated { get; set; }
    [JsonPropertyName("source")]
    public string Source { get; set; } = "";
    [JsonPropertyName("premium_insights_available")]
    public bool PremiumInsightsAvailable { get; set; }
    [JsonPropertyName("spies")] 
    public Spy[] Spies { get; set; } = [];
}