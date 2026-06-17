using FactionBot.Infrastructure.TornApi;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using TornBot.Bot.Infrastructure.TornApi;

namespace TornBot.Bot.Features.Banking;

[SlashCommand("banking", "Commands to interact with the banking system")]
public class BankingCommandModule : ApplicationCommandModule<ApplicationCommandContext>
{
    private TornApiClient _client;

    public BankingCommandModule(TornApiClient client)
    {
        _client = client;
    }

    [SubSlashCommand("request", "put in a request for x amount")]
    public async Task BankRequest(int amount)
    {
        var bankerRoleId = Context.Guild?.Roles.Values.Single(r => r.Name == "banker").Id;
        var user = Context.User as GuildUser;
        var message = await CreateMessageAsync<InteractionMessageProperties>(bankerRoleId!.Value, user!.Id, amount);

        await Context.Interaction.SendResponseAsync(InteractionCallback.Message(message));
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
