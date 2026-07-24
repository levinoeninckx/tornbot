using System.Text.Json.Serialization;

namespace TornBot.Bot.Infrastructure.TornApi.Models;

public class UserDiscordResponse
{
    public Discord Discord { get; set; }
}

public class Discord
{
    [JsonPropertyName("discord_id")] public string DiscordId { get; set; }
    [JsonPropertyName("user_id")] public int UserId { get; set; }
}