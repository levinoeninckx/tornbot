using FactionBot.Infrastructure.TornApi;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using TornBot.Bot.Infrastructure.TornApi;
using TornBot.Bot.Shared;

namespace TornBot.Bot.Features.Banking;

[SlashCommand("banking", "Commands to interact with the banking system")]
public class BankingCommandModule : ApplicationCommandModule<ApplicationCommandContext>
{
    private TornApiClient _client;

    public BankingCommandModule(TornApiClient client)
    {
        _client = client;
    }
    
    // TODO: refactor, createMessgae -> static, return InteractionMessageProperties
    [SubSlashCommand("request", "put in a request for x amount")]
    public async Task<InteractionMessageProperties> BankRequest(int amount)
    {
        if (Context.Guild == null)
        {
            // TODO: add logging
            // TODO: add error message event
            return "something went wrong while processing your request. Please try again later.";
        }
        
        var bankerRole = Context.Guild.Roles.Values.SingleOrDefault(r => r.Name == "Banker");

        if (bankerRole == null)
        {
            return MessageFactory.CreateErrorMessage<InteractionMessageProperties>("Please create the 'Banker' role in the server");
        }
        
        var user = Context.User as GuildUser;
        
        if(user == null)
        {
            return MessageFactory.CreateErrorMessage<InteractionMessageProperties>();
        }
        
        var message = await CreateMessageAsync<InteractionMessageProperties>(bankerRole.Id, user.Id, amount);

        return message;
    }

    private async Task<T> CreateMessageAsync<T>(ulong bankerRoleId, ulong requesteeId, int amount) where T : IMessageProperties, new()
    {
        var requestee = await _client.GetUserProfileByDiscordId(requesteeId);
        var embed = new EmbedProperties()
        {
            Title = "Banking request",
            Description = $"<@&{bankerRoleId}> [{requestee.Name}](https://tcy.sh/p/{requestee.Id}) requested to withdraw some funds from the faction bank",
        };

        var acceptButton = new ButtonProperties($"accept_request:{requesteeId}:{amount}", "Accept", ButtonStyle.Success);
        var declineButton = new ButtonProperties($"decline_request:{requesteeId}", "Decline", ButtonStyle.Danger);

        return new T
        {
            Embeds = [embed],
            AllowedMentions = new AllowedMentionsProperties { AllowedRoles = [bankerRoleId] },
            Components = [new ActionRowProperties{ Components = [acceptButton, declineButton ]}]
        };
    }
}
