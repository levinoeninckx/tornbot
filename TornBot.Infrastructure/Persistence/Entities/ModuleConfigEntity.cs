using System.Text.Json;
using TornBot.Domain.Enums;

namespace TornBot.Infrastructure.Persistence.Entities;

public class ModuleConfigEntity
{
    public int Id { get; set; }
    public ModuleType ModuleType { get; init; }
    public required JsonDocument Config { get; set; }
}