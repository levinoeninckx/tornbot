using TornBot.Bot.Domain.Enums;
using TornBot.Bot.Domain.ValueObjects;

namespace TornBot.Bot.Domain.Models;

public class FactionCrime
{
    public int Id { get; set; }
    public int CrimeId { get; set; }
    public OrganizedCrimeStatus Status { get; set; } = OrganizedCrimeStatus.Recruiting;
    public IList<CrimeSlot> Slots { get; set; } = [];
    public CrimeReward? Reward { get; set; } = new();
    public bool IsAvailable => Status is OrganizedCrimeStatus.Recruiting or OrganizedCrimeStatus.Planning;
    public bool IsCompleted => Status is OrganizedCrimeStatus.Successful or OrganizedCrimeStatus.Failure;
}