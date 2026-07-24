using System.Text.Json;
using TornBot.Domain.Enums;

namespace TornBot.Domain.Models;

public class ModuleConfig
{
    public int Id { get; set; }
    public ModuleType ModuleType { get; set; }
    public JsonDocument Config { get; set; } = null!;
}