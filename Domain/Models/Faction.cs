using System.Text.Json;
using TornBot.Bot.Domain.Enums;

namespace TornBot.Bot.Domain.Models;

public class Faction
{
    public int Id { get; set; }
    public int FactionId { get; set; }
    public ulong GuildId { get; set; }
    public List<ApiKey> ApiKeys { get; set; } = [];
    public List<ModuleConfig> ModuleConfigs { get; }
    public List<FactionCrime> FactionCrimes { get; } = [];
    public List<RetalOpportunity> TrackedAttacks { get; } = [];
    public DateTime CreatedAt { get; private set; }

    public Faction(int factionId, ulong guildId)
    {
        FactionId = factionId;
        GuildId = guildId;
        CreatedAt = DateTime.UtcNow;
        ModuleConfigs = new List<ModuleConfig>
        {
            new(Module.Banking, JsonDocument.Parse(JsonSerializer.Serialize(new BankingModuleConfig()))),
            new(Module.Retal, JsonDocument.Parse(JsonSerializer.Serialize(new RetalModuleConfig()))),
            new(Module.OrganizedCrime, JsonDocument.Parse(JsonSerializer.Serialize(new OrganizedCrimeModuleConfig()))),
            new(Module.Verification, JsonDocument.Parse(JsonSerializer.Serialize(new VerificationModuleConfig())))
        };
    }

    public BankingModuleConfig? BankingModuleConfig => GetModuleConfig<BankingModuleConfig>(Module.Banking);
    public RetalModuleConfig? RetalModuleConfig => GetModuleConfig<RetalModuleConfig>(Module.Retal);

    public VerificationModuleConfig? VerificationModuleConfig =>
        GetModuleConfig<VerificationModuleConfig>(Module.Verification);

    public OrganizedCrimeModuleConfig? OrganizedCrimeModuleConfig =>
        GetModuleConfig<OrganizedCrimeModuleConfig>(Module.OrganizedCrime);

    private T? GetModuleConfig<T>(Module module) where T : class, new()
    {
        var moduleConfig = ModuleConfigs.SingleOrDefault(x => x.Module == module);
        if (moduleConfig != null)
            return moduleConfig.Config.Deserialize<T>();

        return null;
    }
}