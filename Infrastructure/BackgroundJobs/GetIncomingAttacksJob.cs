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
using TornBot.Bot.Infrastructure.TornApi;
using TornBot.Bot.Shared;

namespace TornBot.Bot.Infrastructure.BackgroundJobs;

public class GetIncomingAttacksJob(
    IDbContextFactory<TornbotContext> dbContextFactory,
    IAttackService attackService,
    IPlayerProvider playerProvider,
    TornApiClient tornApiClient,
    NotificationService notificationService,
    ILogger<GetIncomingAttacksJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            var factions = await dbContext.Factions
                .AsSplitQuery()
                .Include(f => f.ModuleConfigs)
                .Include(faction => faction.TrackedAttacks)
                .Include(f => f.ApiKeys)
                .ToListAsync();

            foreach (var faction in factions)
            {
                var trackedAttacksIdHashSet = faction.TrackedAttacks.Select(a => a.AttackId).ToImmutableHashSet();

                var limitedKey = faction.GetKey(AccessLevel.LimitedAccess, requireFactionAccess: true);
                if (limitedKey is null)
                {
                    logger.LogInformation(
                        "Faction with id {factionId} does not have a limited key with faction api access", faction.Id);
                    continue;
                }

                var incomingAttacks = await attackService.GetIncomingAttacksByIdAsync(faction.FactionId, limitedKey);

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
                {
                    logger.LogWarning("No notification parameters found for faction {factionId}",
                        faction.FactionId);
                    continue;
                }

                var publicKey = faction.GetKey(AccessLevel.Public);
                if (publicKey is null)
                {
                    logger.LogWarning("No public api key found for faction with id {FactionId}", faction.FactionId);
                    continue;
                }

                var ffKey = faction.GetKey(AccessLevel.FfScouter);
                if (ffKey is null)
                {
                    logger.LogWarning("No FFScouter key found for faction {factionId}", faction.Id);
                    continue;
                }

                var ffscouterApiKey = new FFScouterApiKey(ffKey.Key, ffKey.TornPlayerId);

                var tsKey = faction.GetKey(AccessLevel.TornStats);
                if (tsKey is null)
                {
                    logger.LogWarning("No TornStats key found for faction {factionId}", faction.Id);
                    continue;
                }

                var tornStatsApiKey = new TornStatApiKey(ffKey.Key, ffKey.TornPlayerId);

                foreach (var newRetal in newRetals)
                {
                    var attacker = await playerProvider.GetPlayerByTornIdAsync(newRetal.AttackerId!.Value, publicKey,
                        ffscouterApiKey, tornStatsApiKey);
                    var defender = await playerProvider.GetPlayerByTornIdAsync(newRetal.DefenderId, publicKey,
                        ffscouterApiKey, tornStatsApiKey);

                    if (attacker is null || defender is null)
                    {
                        logger
                            .LogError(
                                "Could not find attacker {attackerId} or defender {defenderId} for retal {retalId}",
                                newRetal.AttackerId, newRetal.DefenderId, newRetal.Id);
                        continue;
                    }

                    newRetal.Attacker = attacker;
                    newRetal.Defender = defender;

                    var factionBasic =
                        await tornApiClient.GetFactionBasicAsync(attacker.FactionId!.Value, publicKey.Key);
                    var retalMessage = CreateRetalMessageAsync(newRetal, factionBasic);
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

                    logger.LogInformation("new retalopp created with messageId {messageId}", message.Id);
                }
            }

            await dbContext.SaveChangesAsync();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error in {jobName}", nameof(GetIncomingAttacksJob));

            if (context.RefireCount < 3)
                throw new JobExecutionException { RefireImmediately = true };
        }
    }

    private static MessageProperties CreateRetalMessageAsync(Attack attack, FactionBasic? attackerFaction)
    {
        var stringBuilder = new StringBuilder();

        stringBuilder
            .AppendLine(
                $"[{attack.Attacker!.Username}]({ShortUrlHelper.GetProfileUrl(attack.Attacker.Id)}) {attack.Result.ToString().ToLower()} [{attack.Defender.Username}]({ShortUrlHelper.GetProfileUrl(attack.Defender.Id)})");

        stringBuilder.AppendLine("### Player");
        stringBuilder.AppendLine($"[{attack.Attacker.Username}]({ShortUrlHelper.GetProfileUrl(attack.Attacker.Id)})");
        stringBuilder.AppendLine($"Level {attack.Attacker.Level}");

        stringBuilder.AppendLine("### Battle stats");
        var battleStat = attack.Attacker.BattleStat;

        if (battleStat is not null)
        {
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

        if (attack.Attacker.Faction != null && attack.Attacker.FactionId.HasValue)
        {
            stringBuilder.AppendLine("## Faction info");
            stringBuilder.AppendLine(
                $"[{attack.Attacker.Faction.Name}]({ShortUrlHelper.GetFactionUrl(attack.Attacker.FactionId.Value)})");
            stringBuilder.AppendLine($"Respect: {attackerFaction.Respect}");
            stringBuilder.AppendLine($"Rank: {attackerFaction.Rank}");
            stringBuilder.AppendLine($"Member count: {attackerFaction.MemberCount}");
        }
        else
        {
            stringBuilder.AppendLine("** Player not in a faction **");
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