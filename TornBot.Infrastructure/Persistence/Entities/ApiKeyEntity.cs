namespace TornBot.Infrastructure.Persistence.Entities;

public class ApiKeyEntity
{
    public int Id { get; set; }
    public required string Key { get; init; } = "";
    public int TornPlayerId { get; init; }
    public int UsageCount { get; init; } = 0;
    public int AccessLevel { get; init; }
    public bool HasFactionAccess { get; init; }
    public bool HasCompanyAccess { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}