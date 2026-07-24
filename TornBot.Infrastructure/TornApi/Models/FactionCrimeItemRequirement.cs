using System.Text.Json.Serialization;

namespace TornBot.Bot.Infrastructure.TornApi.Models;

public class FactionCrimeItemRequirement
{
    public int Id { get; set; }
    [JsonPropertyName("is_reusable")] public bool IsReusable { get; set; }
    [JsonPropertyName("is_available")] public bool IsAvailable { get; set; }
}