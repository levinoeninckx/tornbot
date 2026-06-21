using System.Globalization;
using FactionBot.Infrastructure.TornApi;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;
using TornBot.Bot.Infrastructure.TornApi;
using TornBot.Bot.Shared;

namespace TornBot.Bot.Features.Banking;

public class BankingButtonInteractionModule : ComponentInteractionModule<ButtonInteractionContext>
{
    private TornApiClient _client;

    public BankingButtonInteractionModule(TornApiClient client)
    {
        _client = client;
    }

    [ComponentInteraction("accept_request")]
    public async Task AcceptBankingRequest(string requesteeId, string requestedAmount)
    {
        var dmChannel = await Context.User.GetDMChannelAsync();
        var guildUser = Context.User as GuildUser;
        var amount = Convert.ToInt32(requestedAmount);

        var requestee = await _client.GetUserProfileByDiscordId(Convert.ToUInt64(requesteeId));
        await dmChannel.SendMessageAsync($"[{requestee.Name}](https://tcy.sh/p/{requestee.Id}) requested {amount.ToString("C0", CultureInfo.GetCultureInfo("en-US"))} from the faction bank");

        var acceptorUser = await _client.GetUserProfileByDiscordId(guildUser!.Id);
        var embed = new EmbedProperties()
        {
            Title = "Banking request accepted",
            Description = $"[{requestee.Name}](https://tcy.sh/p/{requestee.Id})'s request was accepted by [{acceptorUser.Name}](https://tcy.sh/p/{acceptorUser.Id})",
        };
        await Context.Message.ModifyAsync(message => 
        {
            message.Embeds = [embed];
            message.Components = [];
        });
    }
    [ComponentInteraction("decline_request")]
    public async Task DeclineBankingRequest(string requesteeId)
    {
        var guildUser = Context.User as GuildUser;

        if (guildUser == null)
        {
            await Context.Channel.SendMessageAsync(MessageFactory.CreateErrorMessage<MessageProperties>());
            return;
        }
        
        var decliner = await _client.GetUserProfileByDiscordId(guildUser.Id);
        var requestee = await _client.GetUserProfileByDiscordId(Convert.ToUInt64(requesteeId));

        var embed = new EmbedProperties
        {
            Title = "Banking request declined",
            Description = $"[{requestee.Name}](https://tcy.sh/p/{requestee.Id})'s request was declined by [{decliner.Name}](https://tcy.sh/p/{decliner.Id})",
        };

        await Context.Message.ModifyAsync(message => 
        {
            message.Embeds = [embed];
            message.Components = [];
        });
    }
}
