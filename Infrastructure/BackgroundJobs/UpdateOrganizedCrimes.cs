using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Rest;
using TornBot.Bot.Domain.Enums;
using TornBot.Bot.Domain.Models;
using TornBot.Bot.Infrastructure.TornApi;
using TornBot.Bot.Infrastructure.TornApi.Models;
using TornBot.Bot.Shared;

namespace TornBot.Bot.Infrastructure.BackgroundJobs;

public class UpdateOrganizedCrimes(
    TornApiClient client,
    IDbContextFactory<TornbotContext> contextFactory,
    ModuleConfigRepository repository,
    NotificationService notificationService,
    ILogger<UpdateOrganizedCrimes> logger
) : FactionJob<UpdateOrganizedCrimes>(contextFactory, logger)
{
    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("en-US");

    protected override Task<List<Faction>> LoadFactionsAsync(TornbotContext dbContext, CancellationToken ct)
    {
        return dbContext.Factions
            .Include(f => f.OrganizedCrimes)
            .ToListAsync(ct);
    }

    protected override async Task ProcessFactionAsync(Faction faction, CancellationToken ct)
    {
        var config = await repository.GetOrganizedCrimeModuleConfigByGuildId(faction.GuildId);
        if (HasInvalidConfig(config, faction.GuildId)) return;

        var availableCrimes = await client.GetAvailableCrimesByGuildIdAsync(faction.GuildId, ct);
        if (availableCrimes is null)
        {
            Logger.LogError("Could not retrieve available crimes for faction with id {FactionId}", faction.FactionId);
            return;
        }

        await ProcessAvailableCrimes(faction, availableCrimes, config!);

        var completedCrimes = await client.GetCompletedCrimesAsync(faction.GuildId, ct);
        if (completedCrimes is null)
        {
            Logger.LogError("Could not retrieve completed crimes for faction with id {FactionId}", faction.FactionId);
            return;
        }

        var completedTrackedCrimes = completedCrimes
            .Where(c => faction.OrganizedCrimes.Any(crime => crime.CrimeId == c.Id))
            .ToImmutableList();
        await ProcessCompletedCrimes(faction, completedTrackedCrimes, config!);
    }

    private async Task ProcessAvailableCrimes(Faction faction, IEnumerable<FactionCrime> crimes,
        OrganizedCrimeModuleConfig config)
    {
        var trackedCrimeIds = faction.OrganizedCrimes.Select(c => c.CrimeId).ToImmutableHashSet();
        var untrackedCrimes = crimes
            .Where(c => !trackedCrimeIds.Contains(c.Id))
            .ToList();

        faction.OrganizedCrimes.AddRange(untrackedCrimes
            .Select(c => new OrganizedCrime
            {
                CrimeId = c.Id,
                Status = Enum.Parse<OrganizedCrimeStatus>(c.Status)
            })
        );

        var roleId = config.NotificationRoleId!.Value;
        var channelId = config.NotificationChannelId!.Value;
        var embeds = untrackedCrimes.Select(CreateNewCrimeEmbed).ToImmutableList();

        await notificationService.SendEmbedsAsync(channelId, embeds, roleId);
    }

    private async Task ProcessCompletedCrimes(Faction faction, IEnumerable<FactionCrime> crimes,
        OrganizedCrimeModuleConfig config)
    {
        var completedCrimeDict = crimes.ToDictionary(c => c.Id);

        foreach (var trackedCrime in faction.OrganizedCrimes.ToList())
        {
            if (!completedCrimeDict.TryGetValue(trackedCrime.CrimeId, out var completedCrime)) continue;

            var crimeStatus = Enum.Parse<OrganizedCrimeStatus>(completedCrime.Status);
            if (crimeStatus != OrganizedCrimeStatus.Successful &&
                crimeStatus != OrganizedCrimeStatus.Failure) continue;

            var roleId = config.NotificationRoleId!.Value;
            var channelId = config.NotificationChannelId!.Value;

            var message = crimeStatus == OrganizedCrimeStatus.Successful
                ? await CreateSuccessfulMessageAsync(completedCrime)
                : CreateFailureNotification(completedCrime);

            await notificationService.SendNotificationAsync(channelId, message, roleId);
            faction.OrganizedCrimes.Remove(trackedCrime);
        }
    }

    private bool HasInvalidConfig(OrganizedCrimeModuleConfig? config, ulong guildId)
    {
        if (config == null)
        {
            Logger.LogInformation("No organized crimes config found for guild: {GuildId}", guildId);
            return true;
        }

        if (config.NotificationState == ModuleState.Disabled)
        {
            Logger.LogInformation("OC notifications disabled for guild: {GuildId}", guildId);
            return true;
        }

        if (config.NotificationChannelId == null)
        {
            Logger.LogInformation("No notification channel id found for guild: {GuildId}", guildId);
            return true;
        }

        return false;
    }

    private static EmbedProperties CreateNewCrimeEmbed(FactionCrime crime)
    {
        return new EmbedProperties
        {
            Fields =
            [
                new() { Name = "New Crime", Value = crime.Name },
                new() { Name = "Difficulty", Value = crime.Difficulty.ToString() },
                new() { Name = "Slots", Value = FormatSlots(crime.Slots) }
            ],
            Color = new Color(0, 0, 255)
        };
    }

    private async Task<MessageProperties> CreateSuccessfulMessageAsync(FactionCrime crime)
    {
        var players = await Task.WhenAll(crime.Slots
            .Select(slot => client.GetUserProfileById(slot.User!.Id)));

        var playerStringBuilder = new StringBuilder();
        foreach (var player in players)
        {
            playerStringBuilder.AppendLine(player == null
                ? "Unknown player"
                : $"[{player.Name}]({ShortUrlHelper.GetProfileUrl(player.Id)})");
        }

        var rewardsStringBuilder = new StringBuilder();
        if (crime.Rewards.Money > 0)
        {
            rewardsStringBuilder.AppendLine(crime.Rewards.Money.ToString("C0", Culture));
        }

        var itemsInfo = await client.GetItemsInfoAsync(crime.Rewards.Items.Select(i => i.Id));
        if (itemsInfo != null)
        {
            var itemInfoDict = itemsInfo.ToDictionary(i => i.Id);
            foreach (var item in crime.Rewards.Items)
            {
                var name = itemInfoDict.TryGetValue(item.Id, out var info) ? info.Name : "Unknown item";
                rewardsStringBuilder.AppendLine($"{name} x {item.Quantity}");
            }
        }
        else
        {
            rewardsStringBuilder.AppendLine("Unknown items");
        }

        rewardsStringBuilder.AppendLine($"{crime.Rewards.Respect} respect");
        rewardsStringBuilder.AppendLine($"{crime.Rewards.Scope} scope");

        var duration = TimeSpan.FromSeconds(Convert.ToInt32(crime.ExecutedAt - crime.CreatedAt));
        var durationString = duration.TotalSeconds > 0 ? $"{duration.Days} days and {duration.Hours} hours" : "/";
        return new MessageProperties
        {
            Embeds =
            [
                new EmbedProperties
                {
                    Fields =
                    [
                        new() { Name = "Crime Success", Value = crime.Name },
                        new() { Name = "Difficulty", Value = crime.Difficulty.ToString() },
                        new() { Name = "Duration", Value = durationString },
                        new() { Name = "Players", Value = playerStringBuilder.ToString() },
                        new()
                        {
                            Name = "Success chance",
                            Value =
                                $"{CalculateSuccessChance(crime.Slots.Select(c => c.Cpr)).ToString("F2", Culture)}%"
                        },
                        new() { Name = "Rewards", Value = rewardsStringBuilder.ToString() }
                    ],
                    Color = new Color(0, 255, 0)
                }
            ],
        };
    }

    private static MessageProperties CreateFailureNotification(FactionCrime crime)
    {
        return new MessageProperties
        {
            Embeds =
            [
                new EmbedProperties
                {
                    Fields =
                    [
                        new() { Name = "Crime failed", Value = $"{crime.Name}" },
                        new() { Name = "Difficulty", Value = crime.Difficulty.ToString() },
                        new()
                        {
                            Name = "Success chance",
                            Value =
                                $"{CalculateSuccessChance(crime.Slots.Select(c => c.Cpr)).ToString("F2", Culture)}%"
                        }
                    ],
                    Color = new Color(255, 0, 0)
                }
            ],
        };
    }

    private static double CalculateSuccessChance(IEnumerable<int> percentages)
    {
        var percentageList = percentages.ToList();
        if (percentageList.Count == 0) return 0;

        return (double)percentageList.Sum() / percentageList.Count;
    }

    private static string FormatSlots(IEnumerable<FactionCrimeSlot> slots)
    {
        return string.Join(Environment.NewLine, slots
            .GroupBy(s => s.Position)
            .Select(g => $"{g.Key}: x{g.Count()}"));
    }
}