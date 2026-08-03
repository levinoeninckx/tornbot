using Microsoft.Extensions.Logging;
using TornBot.Bot.Domain.Models;
using TornBot.Bot.Domain.ValueObjects;
using TornBot.Bot.Infrastructure.FFScouter;
using TornBot.Bot.Infrastructure.TornApi;
using TornBot.Bot.Infrastructure.TornStats;
using TornBot.Bot.Infrastructure.TornStats.Models;
using PlayerStats = TornBot.Bot.Infrastructure.FFScouter.Models.PlayerStats;

namespace TornBot.Bot.Infrastructure;

public class PlayerProvider(
    TornStatClient tornStatClient, 
    FfScouterClient ffScouterClient, 
    TornApiClient tornClient,
    ILogger<PlayerProvider> logger) : IPlayerProvider
{
    public async Task<Player?> GetPlayerByTornIdAsync(int tornId)
    {
        var player = new Player();
        
        var playerProfile = await tornClient.GetUserProfileById(tornId);
        if (playerProfile is null)
        {
            logger.LogInformation("Player profile not found for Torn ID {TornId}", tornId);
            return null;
        }
        
        var battleStat = await GetPlayerBattlestatsByIdAsync(tornId);

        return new Player
        {
            Id = playerProfile.Id,
            FactionId = playerProfile.FactionId,
            Username = playerProfile.Name,
            Gender = playerProfile.Gender,
            Level = playerProfile.Level,
            BattleStat = battleStat
        };
    }

    private async Task<BattleStat?> GetPlayerBattlestatsByIdAsync(int tornId)
    {
        var spies = await tornStatClient.GetSpyProfileDetailsById(tornId);
        if (spies is not null)
        {
            logger.LogInformation("Spies found for Torn ID {TornId} with tornstats API", tornId);
            return MapProfileDetailsToBattleStat(spies);
        }
        
        var ffScouterStats = await ffScouterClient.GetPlayerStats(tornId);
        if (ffScouterStats is not null)
        {
            logger.LogInformation("Spies found for Torn ID {TornId} with ffscouter API", tornId);
            return MapPlayerStatsToBattleStat(ffScouterStats);
        }

        return null;
    }

    public async Task<Player?> GetPlayerByDiscordIdAsync(ulong discordId)
    {
        var playerProfile = await tornClient.GetUserProfileByDiscordId(discordId);
        if (playerProfile is null)
        {
            logger.LogInformation("Player profile not found for Discord ID {DiscordId}", discordId);
            return null;
        }
        
        var battleStat = await GetPlayerBattlestatsByIdAsync(playerProfile.Id);

        return new Player
        {
            Id = playerProfile.Id,
            FactionId = playerProfile.FactionId,
            Username = playerProfile.Name,
            Gender = playerProfile.Gender,
            Level = playerProfile.Level,
            BattleStat = battleStat
        };
    }
    
    private BattleStat MapProfileDetailsToBattleStat(ProfileDetails spies)
    {
        return new BattleStat
        {
            Estimate = spies.Spy.Total,
            Details = new BattleStatDetails
            {
                Strength = spies.Spy.Strength,
                Defense = spies.Spy.Defense,
                Speed = spies.Spy.Speed,
                Dexterity = spies.Spy.Dexterity
            }
        };
    }

    private BattleStat MapPlayerStatsToBattleStat(PlayerStats ffPlayerStats)
    {
        if (ffPlayerStats.Spies.Length > 0)
        {
            return new BattleStat
            {
                Estimate = ffPlayerStats.Spies[0].Total,
                Details = new BattleStatDetails
                {
                    Strength = ffPlayerStats.Spies[0].Strength,
                    Defense = ffPlayerStats.Spies[0].Defense,
                    Speed = ffPlayerStats.Spies[0].Speed,
                    Dexterity = ffPlayerStats.Spies[0].Dexterity
                }
            };
        }
        
        return new BattleStat
        {
            Estimate = Convert.ToUInt64(ffPlayerStats.BsEstimate),
        };
    }
}