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
    FfScouterClient ffClient,
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
            var guildId = faction.GuildId;
            var config = await repository.GetRetalModuleConfigByGuildId(guildId);
            if (ValidateConfig(config, guildId)) continue;

            if (config!.State == ModuleState.Disabled)
            {
                logger.LogWarning("Retal module is disabled for guild {guildId}", guildId);
                continue;
            }
            
            var trackedAttacks = faction.TrackedAttacks.Select(a => a.AttackId).ToImmutableHashSet();
            var attacks = await attackService.GetIncomingAttacks(guildId);
        
            foreach (var attack in attacks.Where(a => !trackedAttacks.Contains((ulong)a.Id)).Where(a => (DateTime.UtcNow - a.Ended < TimeSpan.FromMinutes(5))))
            {
                if (attack.Attacker == null)
                    continue;
                var attackerBasic = await client.GetUserProfileById(attack.Attacker.Id);
                var defenderBasic = await client.GetUserProfileById(attack.Defender.Id);
                if (attackerBasic == null || defenderBasic == null)
                {
                    logger.LogWarning($"Something went wrong requesting user info for: {attack.Attacker.Id},{attack.Defender.Id}");  
                    continue;
                }

                if (!IsSuccessfulAttack(attack.Result)) continue;
                
                var playerStats = await ffClient.GetPlayerStats(attackerBasic.Id);
                var msg = CreateRetalMessage(attack.Result, attackerBasic, defenderBasic, playerStats);
                
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
            logger.LogError(e, "Failed to save retal opportunities");
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

    private static MessageProperties CreateRetalMessage(AttackResult result, Profile attacker, Profile defender, PlayerStats? attackerStats)
    {
        var stringBuilder = new StringBuilder();
        
        stringBuilder
            .AppendLine($"[{attacker.Name}]({ShortUrlHelper.GetProfileUrl(attacker.Id)}) {result.ToString().ToLower()} [{defender.Name}]({ShortUrlHelper.GetProfileUrl(defender.Id)})");

        if (attackerStats != null)
        {
            stringBuilder.AppendLine("## Attacker stats");
            stringBuilder.AppendLine($"Total Bs: {attackerStats.BsEstimateHuman}");
        
            if (attackerStats.Spies.Length > 0)
            {
                stringBuilder.AppendLine("## Spies");
                stringBuilder.AppendLine($"Strength: {attackerStats.Spies[0].Strength}");
                stringBuilder.AppendLine($"Defense: {attackerStats.Spies[0].Defense}");
                stringBuilder.AppendLine($"Speed: {attackerStats.Spies[0].Speed}");
                stringBuilder.AppendLine($"Dexterity: {attackerStats.Spies[0].Dexterity}");
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