using System.Text.Json.Serialization;

namespace TornBot.Bot.Infrastructure.TornApi.Models;

public class KeyInfoUser
{
    public int Id { get; set; }
    [JsonPropertyName("faction_id")]
    public int FactionId { get; set; }
    [JsonPropertyName("company_id")]
    public int CompanyId { get; set; }
}