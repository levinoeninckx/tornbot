using System.Text.Json.Serialization;

namespace TornBot.Bot.Infrastructure.TornApi.Models;

public class FactionMembersResponse
{
    public List<FactionMember> Members { get; set; } = new();
}

public class FactionMember
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Level { get; set; }

    [JsonPropertyName("has_early_discharge")]
    public bool HasEarlyDischarge { get; set; }

    public required FactionMemberStatus Status { get; set; }
    public long LastAction { get; set; }
}

public class UserResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Level { get; set; }
    public string Status { get; set; } = "";
}