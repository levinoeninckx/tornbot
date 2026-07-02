using TornBot.Bot.Domain.Enums;

namespace TornBot.Bot.Domain.Models;

public class OrganizedCrime
{
    public int Id { get; set; }
    public int CrimeId { get; set; }
    public OrganizedCrimeStatus Status { get; set; } = OrganizedCrimeStatus.Recruiting;
}