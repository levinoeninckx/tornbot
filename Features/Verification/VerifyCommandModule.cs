using Microsoft.EntityFrameworkCore;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using TornBot.Bot.Infrastructure;
using TornBot.Bot.Infrastructure.TornApi;
using TornBot.Bot.Shared;

namespace TornBot.Bot.Features.Verification;

public class VerifyCommandModule(TornApiClient client, VerificationService verificationService) : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("verify", "Verify your torn account with discord")]
    public async Task<InteractionMessageProperties> VerifyUser([SlashCommandParameter] User? user = null)
    {
        
        var guildUser = user == null ? Context.User as GuildUser : user as GuildUser;

        if (guildUser == null)
        {
            return MessageFactory.CreateErrorMessage<InteractionMessageProperties>("User not found.");
        }
        
        var tornUserProfile = await client.GetUserProfileByDiscordId(guildUser.Id);

        var tornNickname = $"{tornUserProfile.Name} [{tornUserProfile.Id}]";

        if (Context.Guild == null)
        {
            return new()
            {
                Content = $"You cannot verify yourself outside of a server, please change this manually to: {tornNickname}",
                Flags = MessageFlags.Ephemeral
            };
        }

        if (Context.Guild.OwnerId == guildUser.Id)
        {
            return new()
            {
                Content = $"You cannot verify yourself as the owner of the server, please change this manually to: {tornNickname}",
                Flags = MessageFlags.Ephemeral
            };
        }
        
        var isVerified = await verificationService.VerifyGuildUserAsync(guildUser);

        if (!isVerified)
        {
            return MessageFactory.CreateErrorMessage<InteractionMessageProperties>("Failed to verify user.");
        }
        
        return new()
        {
            Embeds =
            [
                new()
                {
                    Title = "Verified",
                    Description =
                        $"{guildUser.Username} has been verified as [{tornNickname}](https://tcy.sh/p/{tornUserProfile.Id})"
                }
            ],
        };
    }
}