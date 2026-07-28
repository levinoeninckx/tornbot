using TornBot.Bot.Domain.Enums;

namespace TornBot.Bot.Domain.Models;

public class ApiKey(int tornPlayerId, string key, AccessLevel accessLevel)
{
    public Guid Id { get; private set; }
    public int TornPlayerId { get; private set; } = tornPlayerId;
    public string Key { get; private set; } = key;
    public int UsageCount { get; private set; }
    public AccessLevel AccessLevel { get; private set; } = accessLevel;
    public bool HasFactionAccess { get; set; }
    public bool HasCompanyAccess { get; set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? LastUsed { get; private set; }

    public override string ToString()
    {
        return Key;
    }
}