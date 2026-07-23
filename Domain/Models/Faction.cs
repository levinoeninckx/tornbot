namespace TornBot.Bot.Domain.Models;

public class Faction
{
    public int Id { get; set; }
    public int FactionId { get; set; }
    public ulong GuildId { get; set; }
    public ICollection<ApiKey> ApiKeys { get; set; } = [];
    public ICollection<ModuleConfig> ModuleConfigs { get; set; } = [];
    public List<OrganizedCrime> OrganizedCrimes { get; set; } = [];
    public List<RetalOpportunity> TrackedAttacks { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}