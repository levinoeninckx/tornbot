using TornBot.Bot.Features.Retaliation.Models;
using TornBot.Bot.Infrastructure.FFScouter;
using TornBot.Bot.Infrastructure.TornStats;

namespace TornBot.Bot.Features.Retaliation;

public class BattleStatService(FfScouterClient ffClient, TornStatClient tsClient)
{
    public async Task<BattleStat?> GetUserBattlestatsById(int playerId)
    {
        var tsPlayerStats = await tsClient.GetSpyProfileDetailsById(playerId);

        if (tsPlayerStats is { Status: true, Spy.Status: true })
        {
            return new BattleStat(tsPlayerStats.Spy.Strength, tsPlayerStats.Spy.Defense, tsPlayerStats.Spy.Speed, tsPlayerStats.Spy.Dexterity);
        }

        var ffScouterStats = await ffClient.GetPlayerStats(playerId);
        
        if(ffScouterStats is null)
            return null;

        if (ffScouterStats.Spies.Length <= 0)
            return null;
        
        return new BattleStat(ffScouterStats.Spies[0].Strength, ffScouterStats.Spies[0].Defense, ffScouterStats.Spies[0].Speed, ffScouterStats.Spies[0].Dexterity);       
    }
}