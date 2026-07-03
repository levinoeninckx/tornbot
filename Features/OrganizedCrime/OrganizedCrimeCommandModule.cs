using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Globalization;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using TornBot.Bot.Domain.Enums;
using TornBot.Bot.Infrastructure.TornApi;
using TornBot.Bot.Shared;

namespace TornBot.Bot.Features.OrganizedCrime;


[RequireKey(AccessLevel.Public, false)]
[RequireKey(AccessLevel.Minimal, true)]
[SlashCommand("oc", "organized crime related commands")]
public class OrganizedCrimeCommandModule(TornApiClient client) : ApplicationCommandModule<ApplicationCommandContext>
{

    [SubSlashCommand("profits", "see how much your faction has earned with organized crime")]
    public async Task GetFactionCrimeProfits()
    {
        await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredMessage());
        
        var crimes = await client.GetAllFactionCrimesAsync();

        var factionRewardMoney = crimes.Where(c => c.Status == "Successful").Sum(c => c.Rewards.Money);
        var rewardItems = crimes
            .Where(c => c.Status == "Successful")
            .SelectMany(c => c.Rewards.Items)
            .GroupBy(i => i.Id)
            .ToImmutableDictionary(i => i.Key, i => i.Sum(s => s.Quantity));
        
        var itemsInfo = await client.GetItemsInfoAsync(rewardItems.Keys);
        if (itemsInfo is null)
        {
            return;
        }
        
        var rewardItemsTotalValue = itemsInfo
            .Sum(i => i.Value.MarketPrice * rewardItems[i.Id]);

        var usedItems = crimes
            .Where(c => c.Status == "Successful")
            .SelectMany(c => c.Slots
                .Where(s => s.User is not null && s.User.ItemOutcome is not null)
                .Where(s => s.User!.ItemOutcome!.OwnedBy == "faction")
                .Where(s => s.ItemRequirement is not null && !s.ItemRequirement.IsReusable)
                .Select(s => s.ItemRequirement!.Id))
            .GroupBy(s => s)
            .ToFrozenDictionary(s => s.Key, s => s.Count());
        
        var usedItemsInfo = await client.GetItemsInfoAsync(usedItems.Keys);
        if (usedItemsInfo is null)
        {
            return;
        }
        
        var totalItemCosts = usedItemsInfo.Sum(i => i.Value.MarketPrice * usedItems[i.Id]);

        await Context.Interaction.SendFollowupMessageAsync(new()
        {
            Content =
                $"Total money earned: {((factionRewardMoney + rewardItemsTotalValue) - totalItemCosts).ToString("C0", new CultureInfo("en-US"))}"
        });
    }
}