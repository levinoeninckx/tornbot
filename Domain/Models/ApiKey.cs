using TornBot.Bot.Domain.Enums;

namespace TornBot.Bot.Domain.Models;

public class ApiKey(int tornPlayerId, string key, AccessLevel accessLevel)
{
    public Guid Id { get; private set; }
    public int TornPlayerId { get; private set; } = tornPlayerId;
    public string Key { get; private set; } = key;
    public int UsageCount { get; private set; } = 0;
    public AccessLevel AccessLevel { get; private set; } = accessLevel;
    public bool HasFactionAccess { get; set; } = false;
    public bool HasCompanyAccess { get; set; } = false;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? LastUsed { get; private set; } = null;

    public override string ToString()
    {
        return Key;
    }
}