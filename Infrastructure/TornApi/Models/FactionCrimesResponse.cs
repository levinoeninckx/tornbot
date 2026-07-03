using System.Text.Json.Serialization;

namespace TornBot.Bot.Infrastructure.TornApi.Models;

public class FactionCrimesResponse
{
    public FactionCrime[]? Crimes { get; set; }
}