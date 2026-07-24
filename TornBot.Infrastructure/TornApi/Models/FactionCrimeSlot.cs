using System.Text.Json.Serialization;

namespace TornBot.Bot.Infrastructure.TornApi.Models;

public class FactionCrimeSlot
{
    public string Position { get; set; } = "";

    [JsonPropertyName("checkpoint_pass_rate")]
    public int Cpr { get; set; }

    [JsonPropertyName("item_requirement")] public FactionCrimeItemRequirement? ItemRequirement { get; set; }
    public FactionCrimeUser? User { get; set; }
}