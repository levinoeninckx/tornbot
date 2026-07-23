using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetCord.Rest;
using Quartz;
using TornBot.Bot.Domain.Enums;
using TornBot.Bot.Domain.Models;
using TornBot.Bot.Infrastructure;
using TornBot.Bot.Infrastructure.TornApi;
using TornBot.Bot.Infrastructure.TornApi.Models;
using TornBot.Bot.Shared;

namespace TornBot.Bot.Features.OrganizedCrime.Jobs;

public class UpdateOrganizedCrimes(
    TornApiClient client,
    IDbContextFactory<TornbotContext> contextFactory,
    ModuleConfigRepository repository,
    RestClient restClient,
    ILogger<UpdateOrganizedCrimes> logger
) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            await using var dbContext = await contextFactory.CreateDbContextAsync();
            var factions = await dbContext.Factions
                .Include(f => f.OrganizedCrimes)
                .ToListAsync();

            foreach (var faction in factions)
            {
                var config = await repository.GetOrganizedCrimeModuleConfigByGuildId(faction.GuildId);

                if (ValidateConfig(config, faction.GuildId)) return;

                var availableCrimes = await client.GetAvailableCrimesByGuildIdAsync(faction.GuildId);
                if (availableCrimes is null)
                {
                    logger.LogError("Could not retrieve available crimes for faction with id {factionId}",
                        faction.FactionId);
                    return;
                }

                await ProcessAvailableCrimes(faction, availableCrimes, config!);

                var completedCrimes = await client.GetCompletedCrimesAsync(faction.GuildId);
                if (completedCrimes is null)
                {
                    logger.LogError("Could not retrieve completed crimes for faction with id {factionId}",
                        faction.FactionId);
                    return;
                }

                await ProcessCompletedCrimes(faction, completedCrimes, config!);
            }

            await dbContext.SaveChangesAsync();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Something went wrong while running the UpdateOrganizedCrimes job");
        }
    }

    private async Task ProcessAvailableCrimes(Faction faction, IEnumerable<FactionCrime> crimes,
        OrganizedCrimeModuleConfig config)
    {
        var trackedCrimeIds = faction.OrganizedCrimes.Select(c => c.CrimeId).ToImmutableHashSet();
        var untrackedCrimes = crimes
            .Where(c => c.Status is nameof(OrganizedCrimeStatus.Recruiting) or nameof(OrganizedCrimeStatus.Planning))
            .Where(c => !trackedCrimeIds.Contains(c.Id))
            .ToList();

        if (untrackedCrimes.Count == 0) return;

        faction.OrganizedCrimes.AddRange(untrackedCrimes
            .Select(c => new Domain.Models.OrganizedCrime
            {
                CrimeId = c.Id,
                Status = Enum.Parse<OrganizedCrimeStatus>(c.Status)
            })
        );

        var roleId = config.NotificationRoleId!.Value;
        var channelId = config.NotificationChannelId!.Value;
        var embeds = untrackedCrimes.Select(CreateNewCrimeEmbed).ToList();

        for (var i = 0; i < embeds.Count; i += 10)
        {
            var batch = embeds.Skip(i).Take(10).ToList();
            await restClient.SendMessageAsync(channelId, CreateNewCrimeMessage(batch, roleId));
        }
    }

    private async Task ProcessCompletedCrimes(Faction faction, IEnumerable<FactionCrime> crimes,
        OrganizedCrimeModuleConfig config)
    {
        var crimeDict = crimes.ToDictionary(c => c.Id);
        var trackedCrimes = faction.OrganizedCrimes.ToList();

        foreach (var trackedCrime in trackedCrimes)
        {
            if (!crimeDict.TryGetValue(trackedCrime.CrimeId, out var apiCrime)) continue;

            var currentStatus = Enum.Parse<OrganizedCrimeStatus>(apiCrime.Status);
            if (currentStatus != OrganizedCrimeStatus.Successful &&
                currentStatus != OrganizedCrimeStatus.Failure) continue;
            var roleId = config.NotificationRoleId!.Value;
            var channelId = config.NotificationChannelId!.Value;

            var message = currentStatus == OrganizedCrimeStatus.Successful
                ? await CreateSuccessfulMessageAsync(apiCrime, roleId)
                : CreateFailureMessage(apiCrime, roleId);

            await restClient.SendMessageAsync(channelId, message);
            faction.OrganizedCrimes.Remove(trackedCrime);
        }
    }

    private bool ValidateConfig(OrganizedCrimeModuleConfig? config, ulong guildId)
    {
        if (config == null)
        {
            logger.LogWarning($"No organized crimes config found for guild: {guildId}");
            return true;
        }

        if (config.NotificationState == ModuleState.Disabled)
        {
            logger.LogWarning($"OC notifications disabled for guild: {guildId}");
            return true;
        }

        if (config.NotificationChannelId == null)
        {
            logger.LogWarning($"No notification channel id found for guild: {guildId}");
            return true;
        }

        return false;
    }

    private static EmbedProperties CreateNewCrimeEmbed(FactionCrime crime)
    {
        var stringBuilder = new StringBuilder();

        foreach (var sg in crime.Slots.GroupBy(s => s.Position))
        {
            stringBuilder.AppendLine($"{sg.First().Position}: x{sg.Count()}");
        }

        return new EmbedProperties
        {
            Fields =
            [
                new() { Name = "Crime", Value = crime.Name },
                new() { Name = "Difficulty", Value = crime.Difficulty.ToString() },
                new() { Name = "Slots", Value = stringBuilder.ToString() }
            ]
        };
    }

    private static MessageProperties CreateNewCrimeMessage(IEnumerable<EmbedProperties> embeds,
        ulong ocNotificationRoleId)
    {
        return new MessageProperties
        {
            Content = $"<@&{ocNotificationRoleId}>",
            Embeds = embeds.ToList(),
        };
    }

    private async Task<MessageProperties> CreateSuccessfulMessageAsync(FactionCrime crime, ulong ocNotificationRoleId)
    {
        var stringBuilder = new StringBuilder();

        foreach (var sg in crime.Slots.GroupBy(s => s.Position))
        {
            stringBuilder.AppendLine($"{sg.First().Position}: x{sg.Count()}");
        }

        var playerStringBuilder = new StringBuilder();
        foreach (var slot in crime.Slots)
        {
            var player = await client.GetUserProfileById(slot.User!.Id);
            if (player == null)
            {
                playerStringBuilder.AppendLine("Unknown player");
                continue;
            }

            playerStringBuilder.AppendLine($"[{player.Name}]({ShortUrlHelper.GetProfileUrl(player.Id)})");
        }

        var rewardsStringBuilder = new StringBuilder();
        if (crime.Rewards.Money > 0)
        {
            rewardsStringBuilder.AppendLine($"{crime.Rewards.Money.ToString("C0", new CultureInfo("en-US"))}");
        }

        var itemsInfo = await client.GetItemsInfoAsync(crime.Rewards.Items.Select(i => i.Id));
        if (itemsInfo != null)
        {
            var itemInfoDict = itemsInfo.ToDictionary(i => i.Id);
            foreach (var item in crime.Rewards.Items)
            {
                rewardsStringBuilder.AppendLine($"{itemInfoDict[item.Id].Name} x {item.Quantity}");
            }
        }
        else
        {
            rewardsStringBuilder.AppendLine("Unknown items");
        }

        rewardsStringBuilder.AppendLine($"{crime.Rewards.Respect} respect");
        rewardsStringBuilder.AppendLine($"{crime.Rewards.Scope} scope");

        var duration = crime.ExecutedAt - crime.CreatedAt;
        var durationString = duration.HasValue ? $"{duration.Value.Days} days and {duration.Value.Hours} hours" : "/";
        return new MessageProperties
        {
            Content = $"<@&{ocNotificationRoleId}>",
            Embeds =
            [
                new EmbedProperties
                {
                    Fields =
                    [
                        new() { Name = "Crime", Value = crime.Name },
                        new() { Name = "Difficulty", Value = crime.Difficulty.ToString() },
                        new() { Name = "Duration", Value = durationString },
                        new() { Name = "Players", Value = playerStringBuilder.ToString() },
                        new()
                        {
                            Name = "Success chance",
                            Value =
                                $"{CalculateSuccessChance(crime.Slots.Select(c => c.Cpr)).ToString("F2", new CultureInfo("en-US"))}%"
                        },
                        new() { Name = "Rewards", Value = rewardsStringBuilder.ToString() }
                    ]
                }
            ],
        };
    }

    private static MessageProperties CreateFailureMessage(FactionCrime crime, ulong ocNotificationRoleId)
    {
        return new MessageProperties
        {
            Content = $"<@&{ocNotificationRoleId}>",
            Embeds =
            [
                new EmbedProperties
                {
                    Fields =
                    [
                        new() { Name = "Crime", Value = crime.Name },
                        new() { Name = "Difficulty", Value = crime.Difficulty.ToString() },
                        new()
                        {
                            Name = "Success chance",
                            Value =
                                $"{CalculateSuccessChance(crime.Slots.Select(c => c.Cpr)).ToString("P2", new CultureInfo("en-US"))}%"
                        }
                    ]
                }
            ],
        };
    }

    private static double CalculateSuccessChance(IEnumerable<int> percentages)
    {
        var percentageList = percentages.ToList();
        var total = percentageList.Sum();
        return (double)total / (percentageList.Count * 100);
    }
}