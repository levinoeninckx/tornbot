using System.Text.Json.Serialization;

namespace TornBot.Bot.Features.Retaliation.Models;

public class AttackFull
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public DateTime Started { get; set; }
    public DateTime Ended { get; set; }
    public AttackFullPlayer Attacker { get; set; } = new();
    public AttackFullPlayer Defender { get; set; } = new();
    [JsonPropertyName("respect_gain")]
    public float RespectGain { get; set; }
    [JsonPropertyName("respect_loss")]
    public float RespectLoss { get; set; }
    public AttackResult Result { get; set; }
}

public class AttackFullPlayer
{
    public int Id { get; set; }
    [JsonPropertyName("faction_id")]
    public int? FactionId { get; set; }
}