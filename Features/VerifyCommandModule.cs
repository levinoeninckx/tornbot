using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using TornBot.Bot.Infrastructure.TornApi;

namespace TornBot.Bot.Features;

public class VerifyCommandModule(TornApiClient client) : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("verify", "Verify your torn account with discord")]
    public async Task<InteractionMessageProperties> VerifyUser(User? user = null)
    {
        
        var guildUser = user == null ? Context.User as GuildUser : user as GuildUser;

        if (guildUser == null)
        {
            return "User not found";
        }
        
        var tornUserProfile = await client.GetUserProfileByDiscordId(guildUser.Id);

        var tornNicname = $"[{tornUserProfile.Id}] {tornUserProfile.Name}";

        if (Context.Guild == null)
        {
            return new()
            {
                Content = $"You cannot verify yourself outside of a server, please change this manually to: {tornNicname}",
                Flags = MessageFlags.Ephemeral
            };
        }

        if (Context.Guild.OwnerId == guildUser.Id)
        {
            return new()
            {
                Content = $"You cannot verify yourself as the owner of the server, please change this manually to: {tornNicname}",
                Flags = MessageFlags.Ephemeral
            };
        }

        await Context.Guild.ModifyUserAsync(Context.User.Id,
            properties => properties.Nickname = $"[{tornUserProfile.Id}] {tornUserProfile.Name}");
        
        return new()
        {
            Embeds =
            [
                new()
                {
                    Title = "Verified",
                    Description =
                        $"{Context.User.Username} has been verified as [[{tornUserProfile.Id}] {tornUserProfile.Name}](https://tcy.sh/p/{tornUserProfile.Id})"
                }
            ]
        };
    }
}