using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using TornBot.Bot.Domain.Enums;
using TornBot.Bot.Infrastructure;
using TornBot.Bot.Infrastructure.TornApi;
using TornBot.Bot.Infrastructure.TornApi.Models;
using TornBot.Bot.Shared;

namespace TornBot.Bot.Features.OrganizedCrime;

[RequireKey(AccessLevel.Public, false)]
[RequireKey(AccessLevel.Minimal, true)]
[RequireOrganizedCrimesAllowedRoles]
[RequireOrganizedCrimeRestrictedChannels]
[SlashCommand("oc", "organized crime related commands")]
public class OrganizedCrimeCommandModule(TornApiClient client, IDbContextFactory<TornbotContext> contextFactory)
    : ApplicationCommandModule<ApplicationCommandContext>
{
    [SubSlashCommand("profits", "see how much your faction has earned with organized crime")]
    public async Task GetFactionCrimeProfits()
    {
        await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredMessage());

        await using var context = await contextFactory.CreateDbContextAsync();
        var faction = await context.Factions
            .Include(f => f.ApiKeys)
            .SingleOrDefaultAsync(f => f.GuildId == Context.Guild!.Id);

        if (faction == null)
        {
            await Context.Interaction.SendFollowupMessageAsync(
                MessageFactory.CreateErrorMessage<InteractionMessageProperties>(
                    "This guild is not registered"));
            return;
        }

        var minimalKey = faction.GetKey(AccessLevel.Minimal, requireFactionAccess: true);
        var publicKey = faction.GetKey(AccessLevel.Public);
        if (minimalKey is null || publicKey is null)
        {
            await Context.Interaction.SendFollowupMessageAsync(
                MessageFactory.CreateErrorMessage<InteractionMessageProperties>(
                    "No suitable api key found for this faction"));
            return;
        }

        var crimes = await client.GetCompletedCrimesAsync(minimalKey.Key);
        minimalKey.IncreaseUsage();
        if (crimes == null)
        {
            await context.SaveChangesAsync();
            await Context.Interaction.SendFollowupMessageAsync(
                MessageFactory.CreateErrorMessage<InteractionMessageProperties>(
                    "Something went wrong while contacting the torn api"));
            return;
        }

        var factionRewardMoney = crimes.Where(c => c.Status == "Successful").Sum(c => c.Rewards.Money);
        var rewardItems = crimes
            .Where(c => c.Status == "Successful")
            .SelectMany(c => c.Rewards.Items)
            .ToImmutableList();

        var rewardItemInfo =
            (await client.GetItemsInfoAsync(rewardItems.Select(i => i.Id), publicKey.Key) ?? Array.Empty<TornItem>())
            .ToFrozenDictionary(i => i.Id, i => i);
        publicKey.IncreaseUsage();

        var rewardItemsTotalValue = rewardItems
            .Sum(i => rewardItemInfo[i.Id].Value.MarketPrice);

        var usedItems = crimes
            .Where(c => c.Status == "Successful")
            .SelectMany(c => c.Slots
                .Where(s => s.User is not null && s.User.ItemOutcome is not null)
                .Where(s => s.User!.ItemOutcome!.OwnedBy == "faction")
                .Where(s => s.ItemRequirement is not null && !s.ItemRequirement.IsReusable)
                .Select(s => s.ItemRequirement!.Id))
            .GroupBy(s => s)
            .ToFrozenDictionary(s => s.Key, s => s.Count());

        var usedItemsInfo = await client.GetItemsInfoAsync(usedItems.Keys, publicKey.Key);
        publicKey.IncreaseUsage();
        await context.SaveChangesAsync();
        if (usedItemsInfo is null)
        {
            return;
        }

        var totalItemCosts = usedItemsInfo.Sum(i => i.Value.MarketPrice * usedItems[i.Id]);

        var profitStringBuilder = new StringBuilder();
        profitStringBuilder.AppendLine("### Amount of organized crimes");
        profitStringBuilder.AppendLine($"{crimes.Count}");

        profitStringBuilder.AppendLine("### Money earned");
        profitStringBuilder.AppendLine($"{factionRewardMoney.ToString("C0", new CultureInfo("en-US"))}");

        profitStringBuilder.AppendLine("### Money from items");
        profitStringBuilder.AppendLine($"Items earned: {rewardItems.Count}");
        profitStringBuilder.AppendLine($"{rewardItemsTotalValue.ToString("C0", new CultureInfo("en-US"))}");

        profitStringBuilder.AppendLine("### Cost of used items");
        profitStringBuilder.AppendLine($"Items used: {usedItems.Sum(i => i.Value)}");
        profitStringBuilder.AppendLine($"{totalItemCosts.ToString("C0", new CultureInfo("en-US"))}");

        await Context.Interaction.SendFollowupMessageAsync(new()
        {
            Embeds =
            [
                new EmbedProperties
                {
                    Title = "Profit report",
                    Description = profitStringBuilder.ToString()
                },
            ]
        });
    }
}