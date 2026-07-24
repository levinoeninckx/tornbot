using System.Text.Json.Serialization;

namespace TornBot.Bot.Infrastructure.TornStats.Models;

// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);

// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
public class Attacks
{
    public bool Status { get; set; }
    public string Message { get; set; }
}

public class Compare
{
    public bool Status { get; set; }
    public string Message { get; set; }
}

public class ProfileDetails
{
    public bool Status { get; set; }
    public string Message { get; set; }
    public Compare Compare { get; set; }
    public Spy Spy { get; set; }
    public Attacks Attacks { get; set; }
}

public class Spy
{
    public string Type { get; set; }
    public bool Status { get; set; }
    public string Message { get; set; }
    [JsonPropertyName("player_name")] public string PlayerName { get; set; }
    [JsonPropertyName("player_id")] public int PlayerId { get; set; }
    [JsonPropertyName("player_level")] public int PlayerLevel { get; set; }
    [JsonPropertyName("player_faction")] public string PlayerFaction { get; set; }
    [JsonPropertyName("target_score")] public double TargetScore { get; set; }
    [JsonPropertyName("your_score")] public double YourScore { get; set; }
    [JsonPropertyName("fair_fight_bonus")] public int FairFightBonus { get; set; }
    public string Difference { get; set; }
    public int Timestamp { get; set; }
    public ulong Strength { get; set; }
    public long DeltaStrength { get; set; }

    [JsonPropertyName("strength_timestamp")]
    public int StrengthTimestamp { get; set; }

    public ulong Defense { get; set; }
    public long DeltaDefense { get; set; }

    [JsonPropertyName("defense_timestamp")]
    public int DefenseTimestamp { get; set; }

    public ulong Speed { get; set; }
    public long DeltaSpeed { get; set; }
    [JsonPropertyName("speed_timestamp")] public int SpeedTimestamp { get; set; }
    public ulong Dexterity { get; set; }
    public long DeltaDexterity { get; set; }

    [JsonPropertyName("dexterity_timestamp")]
    public int DexterityTimestamp { get; set; }

    public ulong Total { get; set; }
    public long DeltaTotal { get; set; }
    [JsonPropertyName("total_timestamp")] public int TotalTimestamp { get; set; }
}