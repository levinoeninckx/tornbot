using System.Text.Json.Serialization;
using TornBot.Bot.Domain.Enums;
using TornBot.Bot.Infrastructure.JsonConverters;

namespace TornBot.Bot.Features.Retaliation.Models;

public class AttackFull
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public long Started { get; set; }
    public long Ended { get; set; }
    public AttackFullPlayer? Attacker { get; set; } = new();
    public AttackFullPlayer Defender { get; set; } = new();
    [JsonPropertyName("respect_gain")] public float RespectGain { get; set; }
    [JsonPropertyName("respect_loss")] public float RespectLoss { get; set; }

    [JsonConverter(typeof(AttackResultConverter))]
    public AttackResult Result { get; set; }
}

public class AttackFullPlayer
{
    public int Id { get; set; }
    [JsonPropertyName("faction_id")] public int? FactionId { get; set; }
}