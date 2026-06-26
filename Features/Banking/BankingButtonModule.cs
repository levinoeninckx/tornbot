using System.Globalization;
using NetCord;
using NetCord.Rest;
using NetCord.Services;
using NetCord.Services.ComponentInteractions;
using TornBot.Bot.Infrastructure.TornApi;
using TornBot.Bot.Shared;

namespace TornBot.Bot.Features.Banking;

public class BankingButtonModule(TornApiClient client) : ComponentInteractionModule<ButtonInteractionContext>
{
    [RequireBankerRole]
    [ComponentInteraction("accept_request")]
    public async Task AcceptBankingRequest(string requesteeId, string requestedAmount)
    {
        var dmChannel = await Context.User.GetDMChannelAsync();
        var guildUser = Context.User as GuildUser;
        var amount = Convert.ToInt32(requestedAmount);

        var requestee = await client.GetUserProfileByDiscordId(Convert.ToUInt64(requesteeId));
        await dmChannel.SendMessageAsync($"[{requestee.Name}](https://tcy.sh/p/{requestee.Id}) requested {amount.ToString("C0", CultureInfo.GetCultureInfo("en-US"))} from the faction bank");

        var acceptorUser = await client.GetUserProfileByDiscordId(guildUser!.Id);
        var embed = new EmbedProperties()
        {
            Title = "Banking request accepted",
            Description = $"[{requestee.Name}](https://tcy.sh/p/{requestee.Id})'s request was accepted by [{acceptorUser.Name}](https://tcy.sh/p/{acceptorUser.Id})",
        };
        
        var confirmButton = new ButtonProperties($"confirm_request:{requesteeId}:{amount}", "Confirm", ButtonStyle.Success);
        
        await Context.Message.ModifyAsync(message => 
        {
            message.Embeds = [embed];
            message.Components = [new ActionRowProperties { Components = [confirmButton, new ButtonProperties($"cancel_request:{requesteeId}", "Cancel", ButtonStyle.Danger)] }];
        });

        await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredModifyMessage);
    }
    
    [ComponentInteraction("cancel_request")]
    public async Task CancelBankingRequest(string requesteeId)
    {
        var guildUser = Context.User as GuildUser;

        if (guildUser == null)
        {
            await Context.Channel.SendMessageAsync(MessageFactory.CreateErrorMessage<MessageProperties>());
            return;
        }
        
        var decliner = await client.GetUserProfileByDiscordId(guildUser.Id);
        var requestee = await client.GetUserProfileByDiscordId(Convert.ToUInt64(requesteeId));

        var embed = new EmbedProperties
        {
            Title = "Banking request cancelled",
            Description = $"[{requestee.Name}](https://tcy.sh/p/{requestee.Id})'s request was cancelled by [{decliner.Name}](https://tcy.sh/p/{decliner.Id})",
        };

        await Context.Message.ModifyAsync(message => 
        {
            message.Embeds = [embed];
            message.Components = [];
        });
        
        await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredModifyMessage);
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
        
        var decliner = await client.GetUserProfileByDiscordId(guildUser.Id);
        var requestee = await client.GetUserProfileByDiscordId(Convert.ToUInt64(requesteeId));

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
        
        await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredModifyMessage);
    }

    [RequireBankerRole]
    [ComponentInteraction("confirm_request")]
    public async Task ConfirmBankingRequest(string requesteeId, long amount)
    {
        var guildUser = Context.User as GuildUser;

        if (guildUser == null)
        {
            await Context.Channel.SendMessageAsync(MessageFactory.CreateErrorMessage<MessageProperties>());
            return;
        }
        
        var confirmer = await client.GetUserProfileByDiscordId(guildUser.Id);
        var requestee = await client.GetUserProfileByDiscordId(Convert.ToUInt64(requesteeId));

        var embed = new EmbedProperties
        {
            Title = "Banking request confirmed",
            Description = $"[{requestee.Name}](https://tcy.sh/p/{requestee.Id})'s request was confirmed by [{confirmer.Name}](https://tcy.sh/p/{confirmer.Id})",
        };
        
        var requesteeUser = await Context.Guild!.GetUserAsync(Convert.ToUInt64(requesteeId));
        var dmChannel = await requesteeUser.GetDMChannelAsync();
        var confirmationMessage = MessageFactory.CreateDefaultMessage<MessageProperties>("Request confirmed", $"Your request to withdraw {amount.ToString("C)", CultureInfo.GetCultureInfo("en-US"))} has been confirmed by {guildUser.Nickname}");
        await dmChannel.SendMessageAsync(confirmationMessage);

        await Context.Message.ModifyAsync(message => 
        {
            message.Embeds = [embed];
            message.Components = [];
        });
        
        await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredModifyMessage);
    }
}
