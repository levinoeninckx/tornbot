using System.Text.Json.Serialization;

namespace TornBot.Bot.Infrastructure.TornApi.Models;

public class KeyInfoResponse
{
    [JsonPropertyName("info")] public KeyInfo Info { get; init; }
}