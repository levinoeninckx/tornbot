using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using TornBot.Bot.Domain.Enums;
using TornBot.Bot.Infrastructure.TornApi;
using TornBot.Bot.Shared;

namespace TornBot.Bot.Features;

public class VerifyCommandModule(TornApiClient client, FactionService factionService) : ApplicationCommandModule<ApplicationCommandContext>
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



        var guildRoles = await Context.Guild.GetRolesAsync();
        var factionLink = await factionService.GetFactionByGuildIdAsync(Context.Guild.Id);
        if (factionLink == null)
        {
            return MessageFactory.CreateErrorMessage<InteractionMessageProperties>("Faction not configured. Please use the '/configure faction' command first");
        }
        
        var faction = await client.GetFactionBasicAsync(factionLink.FactionId);
        
            
        await Context.Guild.ModifyUserAsync(guildUser.Id,
            properties =>
            {
                properties
                    .WithNickname(tornNickname);
            });
        
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