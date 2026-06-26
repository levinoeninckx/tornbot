namespace TornBot.Bot.Infrastructure.TornApi.Models;

public class FactionBalanceOverview
{
    public FactionBalance Faction { get; set; }
    public FactionMemberBalance[] Members { get; set; }
}