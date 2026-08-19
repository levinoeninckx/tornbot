// Root myDeserializedClass = JsonSerializer.Deserialize<Root>(myJsonResponse);

using System.Text.Json.Serialization;

namespace TornBot.Bot.Infrastructure.TornStats.Models;

public class Attacks
{
    [JsonPropertyName("status")] public bool? Status { get; set; }

    [JsonPropertyName("message")] public string Message { get; set; }
}

public class AttacksWon
{
    [JsonPropertyName("amount")] public int? Amount { get; set; }

    [JsonPropertyName("difference")] public int? Difference { get; set; }
}

public class ProfileDetails
{
    [JsonPropertyName("status")] public bool? Status { get; set; }

    [JsonPropertyName("data")] public Data? Data { get; set; }

    [JsonPropertyName("timestamp")] public string Timestamp { get; set; }

    [JsonPropertyName("spy")] public Spy Spy { get; set; }

    [JsonPropertyName("attacks")] public Attacks Attacks { get; set; }
}

public class Data
{
    [JsonPropertyName("Xanax Taken")] public XanaxTaken XanaxTaken { get; set; }

    [JsonPropertyName("Refills")] public Refills Refills { get; set; }

    [JsonPropertyName("Stat Enhancers Used")]
    public StatEnhancersUsed StatEnhancersUsed { get; set; }

    [JsonPropertyName("Merits Bought")] public MeritsBought MeritsBought { get; set; }

    [JsonPropertyName("Attacks Won")] public AttacksWon AttacksWon { get; set; }

    [JsonPropertyName("Defends Won")] public DefendsWon DefendsWon { get; set; }

    [JsonPropertyName("Networth")] public Networth Networth { get; set; }
}

public class DefendsWon
{
    [JsonPropertyName("amount")] public int? Amount { get; set; }

    [JsonPropertyName("difference")] public int? Difference { get; set; }
}

public class MeritsBought
{
    [JsonPropertyName("amount")] public int? Amount { get; set; }

    [JsonPropertyName("difference")] public int? Difference { get; set; }
}

public class Networth
{
    [JsonPropertyName("amount")] public ulong? Amount { get; set; }

    [JsonPropertyName("difference")] public ulong? Difference { get; set; }
}

public class Refills
{
    [JsonPropertyName("amount")] public int? Amount { get; set; }

    [JsonPropertyName("difference")] public int? Difference { get; set; }
}

public class Root
{
    [JsonPropertyName("compare")] public ProfileDetails ProfileDetails { get; set; }
}

public class Spy
{
    [JsonPropertyName("type")] public string Type { get; set; }

    [JsonPropertyName("status")] public bool? Status { get; set; }

    [JsonPropertyName("message")] public string Message { get; set; }

    [JsonPropertyName("target_score")] public double? TargetScore { get; set; }

    [JsonPropertyName("your_score")] public double? YourScore { get; set; }

    [JsonPropertyName("fair_fight_bonus")] public int? FairFightBonus { get; set; }

    [JsonPropertyName("difference")] public string Difference { get; set; }

    [JsonPropertyName("strength")] public ulong? Strength { get; set; }

    [JsonPropertyName("deltaStrength")] public ulong? DeltaStrength { get; set; }

    [JsonPropertyName("defense")] public ulong? Defense { get; set; }

    [JsonPropertyName("deltaDefense")] public ulong? DeltaDefense { get; set; }

    [JsonPropertyName("speed")] public ulong? Speed { get; set; }

    [JsonPropertyName("deltaSpeed")] public ulong? DeltaSpeed { get; set; }

    [JsonPropertyName("dexterity")] public ulong? Dexterity { get; set; }

    [JsonPropertyName("deltaDexterity")] public ulong? DeltaDexterity { get; set; }

    [JsonPropertyName("total")] public ulong? Total { get; set; }

    [JsonPropertyName("deltaTotal")] public ulong? DeltaTotal { get; set; }
}

public class StatEnhancersUsed
{
    [JsonPropertyName("amount")] public int? Amount { get; set; }

    [JsonPropertyName("difference")] public int? Difference { get; set; }
}

public class XanaxTaken
{
    [JsonPropertyName("amount")] public int? Amount { get; set; }

    [JsonPropertyName("difference")] public int? Difference { get; set; }
}