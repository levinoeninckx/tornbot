using System;

namespace FactionBot.Infrastructure.TornApi.Models;

public class Faction
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int Score { get; set; }
    public int Chain { get; set; }
}
