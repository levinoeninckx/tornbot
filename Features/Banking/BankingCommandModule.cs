using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using TornBot.Bot.Domain.Enums;
using TornBot.Bot.Infrastructure;
using TornBot.Bot.Infrastructure.TornApi;
using TornBot.Bot.Shared;

namespace TornBot.Bot.Features.Banking;

[RequireKey(AccessLevel.Public, false)]
[RequireKey(AccessLevel.LimitedAccess, true)]
[RequireModuleEnabled(Module.Banking)]
[RequireBankingAllowedRoles]
[RequireBankingChannel]
[SlashCommand("banking", "Commands to interact with the banking system", Contexts = [InteractionContextType.Guild])]
public class BankingCommandModule(
    IDbContextFactory<TornbotContext> contextFactory,
    TornApiClient client,
    ILogger<BankingCommandModule> logger,
    ModuleConfigRepository moduleConfigRepository) : ApplicationCommandModule<ApplicationCommandContext>
{
    [SubSlashCommand("request", "put in a request for x amount")]
    public async Task<InteractionMessageProperties> BankRequest(
        [SlashCommandParameter(Description = "Amount to request (e.g. 10k, 2.5m, 1b)",
            TypeReaderType = typeof(AmountTypeReader))]
        long amount)
    {
        if (Context.Guild == null)
        {
            logger.LogWarning("Guild is null");
            return MessageFactory.CreateErrorMessage<InteractionMessageProperties>(
                "something went wrong while processing your request. Please try again later.");
        }

        var bankingModuleConfig = await moduleConfigRepository.GetBankingModuleConfigByGuildId(Context.Guild.Id);
        if (bankingModuleConfig == null)
        {
            return MessageFactory.CreateErrorMessage<InteractionMessageProperties>(
                "Something went wrong while processing your request. Please try again later.");
        }

        var user = Context.User as GuildUser;

        if (user == null)
        {
            return MessageFactory.CreateErrorMessage<InteractionMessageProperties>();
        }

        await using var context = await contextFactory.CreateDbContextAsync();
        var faction = await context.Factions.SingleOrDefaultAsync(f => f.GuildId == Context.Guild.Id);
        if (faction == null)
            return MessageFactory.CreateErrorMessage<InteractionMessageProperties>("This guild is not registered");

        var memberBalance = await client.GetMemberFactionBalanceByIdAsync(faction.FactionId, user.Id);
        if (memberBalance == null)
        {
            return MessageFactory.CreateErrorMessage<InteractionMessageProperties>(
                "Something went wrong while processing your request. Please try again later.");
        }

        if (memberBalance.Money < amount)
        {
            return MessageFactory.CreateEphermalMessage<InteractionMessageProperties>("Insufficient funds",
                $"You only have {memberBalance.Money.ToString("C0", CultureInfo.CreateSpecificCulture("en-US"))}");
        }

        if (!bankingModuleConfig.BankerRoleId.HasValue)
        {
            return MessageFactory.CreateErrorMessage<InteractionMessageProperties>("Banker role not set");
        }

        var message =
            await CreateMessageAsync<InteractionMessageProperties>(bankingModuleConfig.BankerRoleId.Value, user.Id,
                amount);

        return message;
    }

    [SubSlashCommand("balance", "show your current faction bank balance")]
    public async Task<InteractionMessageProperties> Showbalance()
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var faction = await context.Factions.SingleOrDefaultAsync(f => f.GuildId == Context.Guild!.Id);
        if (faction == null)
            return MessageFactory.CreateErrorMessage<InteractionMessageProperties>("This guild is not registered");

        var balance = await client.GetMemberFactionBalanceByIdAsync(faction.FactionId, Context.User.Id);
        if (balance == null)
        {
            return MessageFactory.CreateErrorMessage<InteractionMessageProperties>(
                "Something went wrong while processing your request. Please try again later.");
        }

        return MessageFactory.CreateEphermalMessage<InteractionMessageProperties>("Balance",
            $"You currently have {balance.Money.ToString("C0", CultureInfo.CreateSpecificCulture("en-US"))} in your faction bank");
    }

    private async Task<T> CreateMessageAsync<T>(ulong bankerRoleId, ulong requesteeId, long amount)
        where T : IMessageProperties, new()
    {
        var requestee = await client.GetUserProfileByDiscordId(requesteeId);
        var embed = new EmbedProperties()
        {
            Title = "Banking request",
            Description =
                $"<@&{bankerRoleId}> [{requestee.Name}]({ShortUrlHelper.GetProfileUrl(requestee.Id)}) requested to withdraw some funds from the faction bank",
        };

        var acceptButton =
            new ButtonProperties($"accept_request:{requesteeId}:{amount}", "Accept", ButtonStyle.Success);
        var cancelButton = new ButtonProperties($"cancel_request:{requesteeId}", "Cancel", ButtonStyle.Danger);

        return new T
        {
            Embeds = [embed],
            AllowedMentions = new AllowedMentionsProperties { AllowedRoles = [bankerRoleId] },
            Components = [new ActionRowProperties { Components = [acceptButton, cancelButton] }]
        };
    }
}