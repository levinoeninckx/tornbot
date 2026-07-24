namespace TornBot.Infrastructure.Persistence.Entities;

public class FactionEntity
{
    public int Id { get; set; }
    public ulong GuildId { get; init; }
    public IList<ApiKeyEntity> ApiKeys { get; init; } = new List<ApiKeyEntity>();
    public IList<ModuleConfigEntity> ModuleConfigs { get; init; } = new List<ModuleConfigEntity>();
    public IList<OrganizedCrimeEntity> OrganizedCrimes { get; init; } = new List<OrganizedCrimeEntity>();
}