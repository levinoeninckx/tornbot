using System.Text.Json.Serialization;

namespace TornBot.Bot.Infrastructure.TornApi.Models;

public class FactionCrimeRewardPayout
{
    public string Type { get; set; } // balance, wallet, inventory
    public float Percentage { get; set; }
    [JsonPropertyName("paid_by")]
    public int PaidBy { get; set; }
    [JsonPropertyName("paid_at")]
    public long PaidAt { get; set; }
    
}