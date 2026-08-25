using TornBot.Bot.Domain.Models;

namespace TornBot.Bot.Infrastructure;

public interface IAttackService
{
    public Task<IReadOnlyList<Attack>> GetOutgoingAttacksByIdAsync(int factionId, ApiKey limitedKey);
    public Task<IReadOnlyList<Attack>> GetIncomingAttacksByIdAsync(int factionId, ApiKey limitedKey);
}
