using Microsoft.Extensions.Logging;
using TornBot.Bot.Domain.Enums;
using TornBot.Bot.Domain.Models;
using TornBot.Bot.Domain.ValueObjects;
using TornBot.Bot.Infrastructure.FFScouter;
using TornBot.Bot.Infrastructure.FFScouter.Models;
using TornBot.Bot.Infrastructure.TornApi;
using TornBot.Bot.Infrastructure.TornStats;
using TornBot.Bot.Infrastructure.TornStats.Models;
using ApiKey = TornBot.Bot.Domain.Models.ApiKey;

namespace TornBot.Bot.Infrastructure;

public class PlayerProvider(
    TornStatClient tornStatClient,
    FfScouterClient ffScouterClient,
    TornApiClient tornClient,
    ILogger<PlayerProvider> logger) : IPlayerProvider
{
    public async Task<Player?> GetPlayerByTornIdAsync(int tornId, ApiKey apiKey)
    {
        var playerProfile = await tornClient.GetUserProfileById(tornId, apiKey.Key);
        apiKey.IncreaseUsage();
        if (playerProfile is null)
        {
            logger.LogInformation("Player profile not found for Torn ID {TornId}", tornId);
            return null;
        }

        var battleStat = await GetPlayerBattlestatsByIdAsync(tornId, apiKey);

        var playerDetails = await tornStatClient.GetSpyProfileDetailsById(tornId, apiKey.Key);
        apiKey.IncreaseUsage();
        if (playerDetails?.Data is null)
        {
            logger.LogInformation("Player details not found for Torn ID {TornId}", tornId);

            return new Player
            {
                Id = playerProfile.Id,
                FactionId = playerProfile.FactionId,
                Username = playerProfile.Name,
                Gender = playerProfile.Gender,
                Level = playerProfile.Level,
                State = Enum.Parse<PlayerState>(playerProfile.Status.State),
                BattleStat = battleStat
            };
        }

        var playerStats = new PlayerStats
        {
            XanaxTaken = Convert.ToInt32(playerDetails.Data?.XanaxTaken.Amount),
            AttacksWon = Convert.ToInt32(playerDetails.Data?.AttacksWon.Amount),
            DefendsWon = Convert.ToInt32(playerDetails.Data?.DefendsWon.Amount),
            MeritsBought = Convert.ToInt32(playerDetails.Data?.MeritsBought.Amount),
            RefillsUsed = Convert.ToInt32(playerDetails.Data?.Refills.Amount),
            StatEnhancersUsed = Convert.ToInt32(playerDetails.Data?.StatEnhancersUsed.Amount),
            Networth = Convert.ToUInt64(playerDetails.Data?.Networth.Amount)
        };

        return new Player
        {
            Id = playerProfile.Id,
            FactionId = playerProfile.FactionId,
            Username = playerProfile.Name,
            Gender = playerProfile.Gender,
            Level = playerProfile.Level,
            State = Enum.Parse<PlayerState>(playerProfile.Status.State),
            BattleStat = battleStat,
            PlayerStats = playerStats
        };
    }

    private async Task<BattleStat?> GetPlayerBattlestatsByIdAsync(int tornId, ApiKey apiKey)
    {
        var spies = await tornStatClient.GetSpyProfileDetailsById(tornId, apiKey.Key);
        apiKey.IncreaseUsage();
        if (spies is not null)
        {
            logger.LogInformation("Spies found for Torn ID {TornId} with tornstats API", tornId);
            return MapProfileDetailsToBattleStat(spies);
        }

        var ffScouterStats = await ffScouterClient.GetPlayerStats(apiKey.Key, tornId);
        apiKey.IncreaseUsage();
        if (ffScouterStats is not null)
        {
            logger.LogInformation("Spies found for Torn ID {TornId} with ffscouter API", tornId);
            return MapPlayerStatsToBattleStat(ffScouterStats);
        }

        return null;
    }

    public async Task<Player?> GetPlayerByDiscordIdAsync(ulong discordId, ApiKey apiKey)
    {
        var playerProfile = await tornClient.GetUserProfileByDiscordId(discordId, apiKey.Key);
        apiKey.IncreaseUsage();
        if (playerProfile is null)
        {
            logger.LogInformation("Player profile not found for Discord ID {DiscordId}", discordId);
            return null;
        }

        var battleStat = await GetPlayerBattlestatsByIdAsync(playerProfile.Id, apiKey);

        return new Player
        {
            Id = playerProfile.Id,
            FactionId = playerProfile.FactionId,
            Username = playerProfile.Name,
            Gender = playerProfile.Gender,
            Level = playerProfile.Level,
            State = Enum.Parse<PlayerState>(playerProfile.Status.State),
            BattleStat = battleStat
        };
    }

    private BattleStat MapProfileDetailsToBattleStat(ProfileDetails spies)
    {
        return new BattleStat
        {
            Estimate = Convert.ToUInt64(spies.Spy.Total),
            Details = new BattleStatDetails
            {
                Strength = Convert.ToUInt64(spies.Spy.Strength),
                Defense = Convert.ToUInt64(spies.Spy.Defense),
                Speed = Convert.ToUInt64(spies.Spy.Speed),
                Dexterity = Convert.ToUInt64(spies.Spy.Dexterity)
            }
        };
    }

    private BattleStat MapPlayerStatsToBattleStat(FfPlayerStats ffFfPlayerStats)
    {
        if (ffFfPlayerStats.Spies.Length > 0)
        {
            return new BattleStat
            {
                Estimate = ffFfPlayerStats.Spies[0].Total,
                Details = new BattleStatDetails
                {
                    Strength = ffFfPlayerStats.Spies[0].Strength,
                    Defense = ffFfPlayerStats.Spies[0].Defense,
                    Speed = ffFfPlayerStats.Spies[0].Speed,
                    Dexterity = ffFfPlayerStats.Spies[0].Dexterity
                }
            };
        }

        return new BattleStat
        {
            Estimate = Convert.ToUInt64(ffFfPlayerStats.BsEstimate),
        };
    }
}