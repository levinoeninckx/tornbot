using System.Text.Json.Serialization;

namespace TornBot.Bot.Infrastructure.TornApi.Models;

public class TornItemValue
{
    // TODO: vendor, buy, sell price skipped
    [JsonPropertyName("market_price")]
    public long MarketPrice { get; set; }
}