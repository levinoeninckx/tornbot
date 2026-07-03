using System.Text.Json.Serialization;

namespace TornBot.Bot.Infrastructure.TornApi.Models;

public class FactionCrimeItemOutcome
{
    [JsonPropertyName("owned_by")]
    public string OwnedBy { get; set; } = ""; // user or faction
    [JsonPropertyName("item_id")]
    public long ItemId { get; set; }
    [JsonPropertyName("item_uid")]
    public long ItemUid { get; set; }
    public string Outcome { get; set; } // used or lost
}