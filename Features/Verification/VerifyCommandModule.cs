using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using TornBot.Bot.Domain.Enums;
using TornBot.Bot.Infrastructure.TornApi;
using TornBot.Bot.Shared;

namespace TornBot.Bot.Features.Verification;

public class VerifyCommandModule(
    TornApiClient client,
    VerificationService verificationService,
    ApiKeyService apiKeyService) : ApplicationCommandModule<ApplicationCommandContext>
{
    [RequireModuleEnabled(Module.Verification)]
    [RequireVerificationChannels]
    [RequireVerificationRoles]
    [SlashCommand("verify", "Verify your torn account with discord", Contexts = [InteractionContextType.Guild])]
    public async Task<InteractionMessageProperties> VerifyUser([SlashCommandParameter] User? user = null)
    {
        var guildUser = user == null ? Context.User as GuildUser : user as GuildUser;

        if (guildUser == null)
        {
            return MessageFactory.CreateErrorMessage<InteractionMessageProperties>("User not found.");
        }

        var apiKey = await apiKeyService.GetPublicApiKeyAsync();
        if (apiKey is null)
        {
            return MessageFactory.CreateErrorMessage<InteractionMessageProperties>("No public api key found.");
        }

        var tornUserProfile = await client.GetUserProfileByDiscordId(guildUser.Id, apiKey);

        var tornNickname = $"{tornUserProfile.Name} [{tornUserProfile.Id}]";

        if (Context.Guild == null)
        {
            return new()
            {
                Content =
                    $"You cannot verify yourself outside of a server, please change this manually to: {tornNickname}",
                Flags = MessageFlags.Ephemeral
            };
        }

        if (Context.Guild.OwnerId == guildUser.Id)
        {
            return new()
            {
                Content =
                    $"You cannot verify yourself as the owner of the server, please change this manually to: {tornNickname}",
                Flags = MessageFlags.Ephemeral
            };
        }

        var userProfile = await verificationService.VerifyGuildUserAsync(guildUser);

        if (userProfile == null)
        {
            return MessageFactory.CreateErrorMessage<InteractionMessageProperties>(
                "Failed to verify user, try again later.");
        }

        return new()
        {
            Embeds =
            [
                new()
                {
                    Title = "Verified",
                    Description =
                        $"{guildUser.Username} has been verified as [{tornNickname}]({ShortUrlHelper.GetProfileUrl(tornUserProfile.Id)})"
                }
            ],
        };
    }

    [RequireVerificationChannels]
    [RequireVerificationRoles]
    [SlashCommand("verifyall", "verify all users in the server")]
    public async Task<InteractionMessageProperties> VerifyAllUsers()
    {
        var users = Context.Guild!.Users.Where(u => !u.Value.IsBot).Select(u => u.Value).ToList();
        var verificationTasks = users.Select(verificationService.VerifyGuildUserAsync).ToList();

        await Task.WhenAll(verificationTasks);

        return MessageFactory.CreateDefaultMessage<InteractionMessageProperties>("Verification complete",
            "All users have been verified");
    }
}