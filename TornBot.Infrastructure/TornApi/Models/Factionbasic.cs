using System.Text.Json.Serialization;

namespace TornBot.Bot.Infrastructure.TornApi.Models;

public class Factionbasic
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Tag { get; set; } = "";
    [JsonPropertyName("tag_image")] public string TagImage { get; set; } = "";
    [JsonPropertyName("banner_image")] public string BannerImage { get; set; } = "";
    [JsonPropertyName("leader_id")] public int LeaderId { get; set; }
    [JsonPropertyName("co_leader_id")] public int co_leader_id { get; set; }
    public int Respect { get; set; }
    [JsonPropertyName("days_old")] public int DaysOld { get; set; }
    public int Capacity { get; set; }
    public int Members { get; set; }
    [JsonPropertyName("is_enlisted")] public bool? IsEnlisted { get; set; }
    public FactionRank Rank { get; set; } = new();
    [JsonPropertyName("best_chain")] public int BestChain { get; set; }
    public string Note { get; set; } = "";
}