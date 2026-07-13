namespace TornBot.Bot.Domain.Models;

public class Faction
{
    public int Id { get; set; }
    public int FactionId { get; set; }
    public ulong GuildId { get; set; }
    public HashSet<ApiKey> ApiKeys { get; set; } = [];
    public HashSet<ModuleConfig> ModuleConfigs { get; set; } = [];
    public HashSet<OrganizedCrime> OrganizedCrimes { get; set; } = [];
    public List<RetalOpportunity> TrackedAttacks { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}