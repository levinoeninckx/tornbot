using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Rest;
using Quartz;
using TornBot.Bot.Domain.Enums;
using TornBot.Bot.Domain.Models;
using TornBot.Bot.Shared;

namespace TornBot.Bot.Infrastructure.BackgroundJobs;

public class GetIncomingAttacksJob(
    IDbContextFactory<TornbotContext> dbContextFactory,
    IAttackService attackService,
    IPlayerProvider playerProvider,
    NotificationService notificationService,
    ILogger<GetIncomingAttacksJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            var factions = await dbContext.Factions
                .Include(f => f.ModuleConfigs)
                .Include(faction => faction.TrackedAttacks)
                .ToListAsync();

            foreach (var faction in factions)
            {
                var trackedAttacksIdHashSet = faction.TrackedAttacks.Select(a => a.AttackId).ToImmutableHashSet();
                var incomingAttacks = await attackService.GetIncomingAttacksByIdAsync(faction.FactionId);

                var newRetals = incomingAttacks
                    .Where(a => !trackedAttacksIdHashSet.Contains(a.Id))
                    .Where(a => a.CanBeRetaliated())
                    .OrderBy(a => a.Timestamp)
                    .ToImmutableList();

                logger.LogInformation("Found {count} new retal opportunities for faction {factionId}", newRetals.Count,
                    faction.FactionId);

                var notificationParameters =
                    GetNotificationParameters(faction.ModuleConfigs.Single(c => c.Module == Module.Retal));
                if (notificationParameters is null)
                    continue;

                foreach (var newRetal in newRetals)
                {
                    var attacker =
                        await playerProvider.GetPlayerByTornIdAsync(newRetal.AttackerId!.Value, faction.FactionId);
                    var defender = await playerProvider.GetPlayerByTornIdAsync(newRetal.DefenderId, faction.FactionId);

                    if (attacker is null || defender is null)
                    {
                        logger.LogInformation("Could not find attacker or defender for retal {retalId}", newRetal.Id);
                        continue;
                    }

                    newRetal.Attacker = attacker;
                    newRetal.Defender = defender;

                    var retalMessage = CreateRetalMessageAsync(newRetal);
                    var notificationCommand = new NotificationCommand
                    {
                        ChannelId = notificationParameters.ChannelId,
                        RoleId = notificationParameters.RoleId,
                        MessageProperties = retalMessage
                    };

                    var message = await notificationService.SendNotificationAsync(notificationCommand);

                    var retalOpp = new RetalOpportunity
                    {
                        MessageId = message.Id,
                        TargetPlayerId = newRetal.AttackerId.Value,
                        AttackId = newRetal.Id,
                        Timestamp = newRetal.Timestamp,
                        State = RetalOpportunityState.Open
                    };

                    faction.TrackedAttacks.Add(retalOpp);
                }

                await dbContext.SaveChangesAsync();
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error in {jobName}", nameof(GetIncomingAttacksJob));
        }
    }

    private static MessageProperties CreateRetalMessageAsync(Attack attack)
    {
        var stringBuilder = new StringBuilder();

        stringBuilder
            .AppendLine(
                $"[{attack.Attacker!.Username}]({ShortUrlHelper.GetProfileUrl(attack.Attacker.Id)}) {attack.Result.ToString().ToLower()} [{attack.Defender.Username}]({ShortUrlHelper.GetProfileUrl(attack.Defender.Id)})");

        stringBuilder.AppendLine("### Player");
        stringBuilder.AppendLine($"[{attack.Attacker.Username}]({ShortUrlHelper.GetProfileUrl(attack.Attacker.Id)})");
        stringBuilder.AppendLine($"Level {attack.Attacker.Level}");

        if (attack.Attacker.Faction != null && attack.Attacker.FactionId.HasValue)
        {
            stringBuilder.AppendLine(
                $"[{attack.Attacker.Faction.Name}]({ShortUrlHelper.GetFactionUrl(attack.Attacker.FactionId.Value)})");
        }

        stringBuilder.AppendLine("### Battle stats");
        if (attack.Attacker.BattleStat is not null)
        {
            var battleStat = attack.Attacker.BattleStat;
            stringBuilder.AppendLine($"Total: {battleStat.Estimate.ToHumanReadable()}");

            if (battleStat.Details is not null)
            {
                stringBuilder.AppendLine($"Strength {battleStat.Details.Strength.ToHumanReadable()}");
                stringBuilder.AppendLine($"Defense {battleStat.Details.Defense.ToHumanReadable()}");
                stringBuilder.AppendLine($"Speed {battleStat.Details.Speed.ToHumanReadable()}");
                stringBuilder.AppendLine($"Dexterity {battleStat.Details.Dexterity.ToHumanReadable()}");
            }
        }
        else
        {
            stringBuilder.AppendLine("** No battle stats found **");
        }

        return new MessageProperties
        {
            Embeds =
            [
                new EmbedProperties
                {
                    Title = "Retaliation opportunity",
                    Description = stringBuilder.ToString(),
                    Color = new Color(0, 0, 255)
                },
            ],
            Components =
            [
                new ActionRowProperties
                {
                    new LinkButtonProperties(ShortUrlHelper.GetAttackUrl(attack.Attacker.Id).ToString(), "Attack"),
                    new LinkButtonProperties(ShortUrlHelper.GetProfileUrl(attack.Attacker.Id).ToString(), "Profile")
                }
            ],
        };
    }

    private static NotificationParameters? GetNotificationParameters(ModuleConfig moduleConfig)
    {
        var retalModuleConfig = moduleConfig.Config.Deserialize<RetalModuleConfig>();
        if (retalModuleConfig is null)
            return null;

        if (retalModuleConfig.State != ModuleState.Enabled)
            return null;

        if (!retalModuleConfig.NotificationChannelId.HasValue || !retalModuleConfig.NotificationRoleId.HasValue)
            return null;

        return new NotificationParameters(retalModuleConfig.NotificationChannelId.Value,
            retalModuleConfig.NotificationRoleId.Value);
    }

    private class NotificationParameters(ulong channelId, ulong roleId)
    {
        public ulong ChannelId { get; } = channelId;
        public ulong RoleId { get; } = roleId;
    }
}