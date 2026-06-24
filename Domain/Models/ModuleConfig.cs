using System.Text.Json;
using TornBot.Bot.Domain.Enums;

namespace TornBot.Bot.Domain.Models;

public class ModuleConfig
{
    public int Id { get; set; }
    public Module Module { get; set; }
    public JsonDocument Config { get; set; } = null!;
}