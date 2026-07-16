using System.Collections.Immutable;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetCord.Rest;
using Quartz;
using TornBot.Bot.Domain.Enums;
using TornBot.Bot.Domain.Models;
using TornBot.Bot.Features.Retaliation.Models;
using TornBot.Bot.Infrastructure;
using TornBot.Bot.Infrastructure.FFScouter;
using TornBot.Bot.Infrastructure.FFScouter.Models;
using TornBot.Bot.Infrastructure.TornApi;
using TornBot.Bot.Infrastructure.TornApi.Models;
using TornBot.Bot.Shared;

namespace TornBot.Bot.Features.Retaliation;

public class GetIncomingAttacks(
    AttackService attackService,
    IDbContextFactory<TornbotContext> contextFactory,
    TornApiClient client,
    BattleStatService bsService,
    ModuleConfigRepository repository, 
    RestClient restClient,
    ILogger<GetIncomingAttacks> logger
    ) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var factions = await dbContext.Factions
            .Include(f => f.TrackedAttacks)
            .ToListAsync();

        foreach (var faction in factions)
        {
            var config = await repository.GetRetalModuleConfigByGuildId(faction.GuildId);
            if (ValidateConfig(config, faction.GuildId)) continue;

            if (config!.State == ModuleState.Disabled)
            {
                logger.LogWarning("Retal module is disabled for guild {guildId}", faction.GuildId);
                continue;
            }
            
            var trackedAttacks = faction.TrackedAttacks.Select(a => a.AttackId).ToImmutableHashSet();
            var attacks = await attackService.GetIncomingAttacks(faction.GuildId);
        
            foreach (var attack in attacks.Where(a => !trackedAttacks.Contains((ulong)a.Id)).Where(a => (DateTime.UtcNow - a.Ended < TimeSpan.FromMinutes(5))))
            {
                if (attack.Attacker == null)
                    continue;
                
                if (attack.Attacker.FactionId == attack.Defender.FactionId)
                {
                    logger.LogInformation("Attacker and defender from same faction with Id {factionId}", attack.Attacker.FactionId);
                    continue;
                }
                
                var attackerBasic = await client.GetUserProfileById(attack.Attacker.Id);
                var defenderBasic = await client.GetUserProfileById(attack.Defender.Id);
                if (attackerBasic == null || defenderBasic == null)
                {
                    logger.LogWarning("Something went wrong requesting user info for: {attackerId},{defenderId}", attack.Attacker.Id, attack.Defender.Id);  
                    continue;
                }

                if (!IsSuccessfulAttack(attack.Result)) continue;

                var playerStats = await bsService.GetUserBattlestatsById(attackerBasic.Id);
                var msg = await CreateRetalMessageAsync(attack.Result, attackerBasic, defenderBasic, playerStats);
                
                var message = await restClient.SendMessageAsync(config.NotificationChannelId!.Value, msg);
                var trackedAttack = new RetalOpportunity
                {
                    AttackId = (ulong)attack.Id,
                    MessageId = message.Id,
                    TargetPlayerId = attack.Attacker.Id
                };
                        
                faction.TrackedAttacks.Add(trackedAttack);
            }
        }

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (Exception e)
        {
            logger.LogCritical(e, "Failed to save retal opportunities");
        }
    }

    private static bool IsSuccessfulAttack(AttackResult result) => result switch
    {
        AttackResult.Attacked or
            AttackResult.Mugged or
            AttackResult.Hospitalized or
            AttackResult.Arrested or
            AttackResult.Bounty => true,
        _ => false
    };
    
    private bool ValidateConfig(RetalModuleConfig? config, ulong guildId)
    {
        if(config == null)
        {
            logger.LogWarning("No retal module config found for guild {guildId}", guildId);
            return true;
        }

        if (!config.NotificationChannelId.HasValue)
        {
            logger.LogWarning("No retal channel id set for retal module for guild {guildId}", guildId);
            return true;
        }

        return false;
    }

    private async Task<MessageProperties> CreateRetalMessageAsync(AttackResult result, Profile attacker, Profile defender, BattleStat? battleStat)
    {
        // TODO: refactor to be static?
        var stringBuilder = new StringBuilder();
        
        stringBuilder
            .AppendLine($"[{attacker.Name}]({ShortUrlHelper.GetProfileUrl(attacker.Id)}) {result.ToString().ToLower()} [{defender.Name}]({ShortUrlHelper.GetProfileUrl(defender.Id)})");
        stringBuilder.Append('\n');
        
        var faction = await client.GetUserFactionAsync(attacker.Id);
        if (faction is not null)
        {
            stringBuilder.AppendLine($"[{faction.Name}]({ShortUrlHelper.GetFactionUrl(faction.Id)})");
        }
        
        if (battleStat != null)
        {
            stringBuilder.Append('\n');
            stringBuilder.AppendLine("## Player stats");
            stringBuilder.AppendLine($"Total Bs: {battleStat.TotalHumanReadable}");

            if (battleStat.Details is not null)
            {
                stringBuilder.AppendLine($"Strength {battleStat.Details.StrengthHumanReadable}");
                stringBuilder.AppendLine($"Defense {battleStat.Details.DefenseHumanReadable}");
                stringBuilder.AppendLine($"Speed {battleStat.Details.SpeedHumanReadable}");
                stringBuilder.AppendLine($"Dexterity {battleStat.Details.DexterityHumanReadable}");
            }
        }
        else
        {
            stringBuilder.AppendLine("## No stats available");
        }
        
        return new MessageProperties
        {
            Embeds = [
                new()
                {
                    Title = "Retaliation opportunity",
                    Description = stringBuilder.ToString(),
                },
            ],
            Components = [
                new ActionRowProperties
                {
                    new LinkButtonProperties(ShortUrlHelper.GetAttackUrl(attacker.Id).ToString(), "Attack"),
                    new LinkButtonProperties(ShortUrlHelper.GetProfileUrl(attacker.Id).ToString(), "Profile")
                }
            ]
        };
    }
}