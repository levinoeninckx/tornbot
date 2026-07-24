using System.Text.Json;
using TornBot.Domain.Enums;
using TornBot.Domain.Models;

namespace TornBot.Domain.Repositories;

public interface IModuleConfigRepository
{
    public Task<BankingModuleConfig?> GetBankingModuleConfigByFactionIdAsync(int factionId);
    public Task<RetalModuleConfig?> GetRetalModuleConfigByFactionIdAsync(int factionId);
    public Task<OrganizedCrimeModuleConfig?> GetOrganizedCrimeModuleConfigByFactionIdAsync(int factionId);
    public Task<VerificationConfig?> GetVerificationConfigByFactionIdAsync(int factionId);
    public Task<bool> UpdateModuleConfig(ulong guildId, ModuleType module, JsonDocument jsonConfig);
}