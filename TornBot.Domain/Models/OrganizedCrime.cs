using TornBot.Domain.Enums;

namespace TornBot.Domain.Models;

public class OrganizedCrime
{
    public int Id { get; set; }
    public int CrimeId { get; set; }
    public OrganizedCrimeStatus Status { get; set; } = OrganizedCrimeStatus.Recruiting;
}