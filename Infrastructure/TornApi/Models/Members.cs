using System.Text.Json.Serialization;

namespace TornBot.Bot.Infrastructure.TornApi.Models;

public class FactionMembersResponse
{
    public List<FactionMemberDto> Members { get; set; } = new();
}

public class FactionMemberDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Level { get; set; }
    [JsonPropertyName("days_in_faction")] public int DaysInFaction { get; set; }
    [JsonPropertyName("is_revivable")] public bool IsRevivable { get; set; }
    [JsonPropertyName("is_on_wall")] public bool IsOnWall { get; set; }
    [JsonPropertyName("is_in_oc")] public bool IsInOc { get; set; }

    [JsonPropertyName("has_early_discharge")]
    public bool HasEarlyDischarge { get; set; }

    public required FactionMemberStatus Status { get; set; }
    [JsonPropertyName("last_action")] public required FactionMemberLastAction LastAction { get; set; }
    [JsonPropertyName("revive_setting")] public ReviveSetting ReviveSetting { get; set; }
}

public enum ReviveSetting
{
    Everyone,
    FriendsAndFaction,
    NoOne,
    Unknown
}

public class FactionMemberStatus
{
    public required string Description { get; set; }
    public string? Details { get; set; }
    public required string State { get; set; }
    public required string Color { get; set; }
    public int? Until { get; set; }
}

public enum FactionMemberState
{
    Abroad,
    Awoken,
    Dormant,
    Fallen,
    Federal,
    Hospital,
    Jail,
    Okay,
    Traveling,
}

public class FactionMemberLastAction
{
    public FactionMemberLastActionStatus Status { get; set; }
    public int Timestamp { get; set; }
    public string Relative { get; set; } = "";
}

public enum FactionMemberLastActionStatus
{
    Online,
    Idle,
    Offline
}

public class UserResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Level { get; set; }
    public string Status { get; set; } = "";
}