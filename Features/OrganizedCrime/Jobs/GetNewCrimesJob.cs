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

namespace TornBot.Bot.Features.OrganizedCrime.Jobs;

public class GetNewCrimesJob(TornApiClient client, IDbContextFactory<TornbotContext> contextFactory, ModuleConfigRepository repository, RestClient restClient, ILogger<GetNewCrimesJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        if (context.RefireCount > 10)
        {
            logger.LogWarning("GetNewCrimesJob has been refired more than 10 times");
            return;
        }

        try
        {
            var guildId = Convert.ToUInt64(context.MergedJobDataMap.GetString("guildId"));
            var config = await repository.GetOrganizedCrimeModuleConfigByGuildId(guildId);
            
            if (config == null)
            {
                logger.LogWarning($"No organized crimes config found for guild: {guildId}");
                return;
            }
            
            if (config.NotificationChannelId == null)
            {
                logger.LogWarning($"No notification channel id found for guild: {guildId}");
                return;
            }
            
            if (config.NotificationRoleId == null)
            {
                return;
            }
            
            var crimes = await client.GetFactionCrimesAsync();
            
            await ProcessNewCrimes(crimes, config);
            await ProcessSuccessfulCrimes(crimes, config);

        }
        catch (Exception e)
        {
            logger.LogError(e, "Error getting new crimes");
            throw new JobExecutionException(cause: e, refireImmediately: true);
        }
    }

    private async Task ProcessNewCrimes(IEnumerable<FactionCrime> crimes, OrganizedCrimeModuleConfig config)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var trackedCrimes = await dbContext.OrganizedCrimes.ToListAsync();

        var newCrimes = crimes.Where(c => trackedCrimes
                .All(tc => tc.Id != c.Id))
            .Where(c => c.Status == nameof(OrganizedCrimeStatus.Recruiting));
        if (config.NotificationChannelId == null || config.NotificationRoleId == null) return;
        foreach (var crime in newCrimes)
        {
            await restClient.SendMessageAsync(config.NotificationChannelId.Value, CreateNewCrimeMessage(crime, config.NotificationRoleId.Value));
        }
    }
    
    private static MessageProperties CreateNewCrimeMessage(FactionCrime crime, ulong ocNotificationRoleId)
    {
        var stringBuilder = new StringBuilder();

        foreach (var sg in crime.Slots.GroupBy(s => s.Position))
        {
            stringBuilder.AppendLine($"{sg.First().Position}: x{sg.Count()}");
        }
        
        return new MessageProperties
        {
            Content = $"<@&{ocNotificationRoleId}>",
            Embeds = [
                new EmbedProperties
                {
                    Fields = [
                        new() { Name = "Crime", Value = crime.Name },
                        new() { Name = "Difficulty", Value = crime.Difficulty.ToString() },
                        new() { Name = "Slots", Value = stringBuilder.ToString() }
                    ]
                }
            ],
        };
    }

    private async Task ProcessSuccessfulCrimes(IEnumerable<FactionCrime> crimes, OrganizedCrimeModuleConfig config)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var trackedCrimes = await dbContext.OrganizedCrimes.ToListAsync();
        
        var successfulCrimes = crimes
            .Where(c => trackedCrimes.Any(tc => tc.Id == c.Id))
            .Where(c => c.Status == nameof(OrganizedCrimeStatus.Successful))
            .ToList();
        foreach (var crime in successfulCrimes)
        {
            
        }
    }
    
    private async Task<MessageProperties> CreateSuccessfulMessageAsync(FactionCrime crime, ulong ocNotificationRoleId)
    {
        var stringBuilder = new StringBuilder();

        foreach (var sg in crime.Slots.GroupBy(s => s.Position))
        {
            stringBuilder.AppendLine($"{sg.First().Position}: x{sg.Count()}");
        }

        var duration = TimeSpan.FromSeconds(Math.Abs(crime.ExecutedAt!.Value - crime.CreatedAt));
        
        var playerStringBuilder = new StringBuilder();
        foreach (var slot in crime.Slots)
        {
            var player = await client.GetUserProfileById(slot.User!.Id);
            if (player == null)
            {
                playerStringBuilder.AppendLine("Unknown player");
                continue;
            }
            
            playerStringBuilder.AppendLine($"[{player.Name}](https://tcy.sh/p/{player.Id})");
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

        return new MessageProperties
        {
            Content = $"<@&{ocNotificationRoleId}>",
            Embeds = [
                new EmbedProperties
                {
                    Fields = [
                        new() { Name = "Crime", Value = crime.Name },
                        new() { Name = "Difficulty", Value = crime.Difficulty.ToString() },
                        new() { Name = "Duration", Value = $"{duration.Days} days and {duration.Hours} hours" },
                        new(){ Name = "Players", Value = playerStringBuilder.ToString()},
                        new() { Name = "Success chance", Value = $"{CalculateSuccessChance(crime.Slots.Select(c => c.Cpr))}%"},
                        new (){ Name = "Rewards", Value = rewardsStringBuilder.ToString()}
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
            Embeds = [
                new EmbedProperties
                {
                    Fields = [
                        new() { Name = "Crime", Value = crime.Name },
                        new() { Name = "Difficulty", Value = crime.Difficulty.ToString() },
                        new() { Name = "Success chance", Value = $"{CalculateSuccessChance(crime.Slots.Select(c => c.Cpr))}%"},
                    ]
                }
            ],
        };
    }
    
    private static double CalculateSuccessChance(IEnumerable<int> percentages)
    {
        double totalFailChance = 1.0;

        foreach (var chance in percentages)
        {
            double successDecimal = chance / 100.0;
        
            totalFailChance *= (1.0 - successDecimal);
        }

        return (1.0 - totalFailChance) * 100.0;
    }
}