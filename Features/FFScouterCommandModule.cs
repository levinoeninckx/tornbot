using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using TornBot.Bot.Domain.Enums;
using TornBot.Bot.Domain.Models;
using TornBot.Bot.Infrastructure;
using TornBot.Bot.Infrastructure.FFScouter;
using TornBot.Bot.Infrastructure.TornApi;
using TornBot.Bot.Shared;

namespace TornBot.Bot.Features;

[SlashCommand("ff", "ffScouter commands", DefaultGuildPermissions = Permissions.Administrator)]
public class FfScouterCommandModule(TornApiClient client, FfScouterClient ffClient, IDbContextFactory<TornbotContext> contextFactory, ILogger<FfScouterCommandModule> logger) : ApplicationCommandModule<ApplicationCommandContext>
{
    [SubSlashCommand("key", "set the api key you want to use")]
    public async Task<InteractionMessageProperties> SetApiKey([SlashCommandParameter] string key)
    {
        var isValid = await ffClient.IsApiKeyValid(key);

        if (isValid == false)
        {
            return MessageFactory.CreateErrorMessage<InteractionMessageProperties>("Key is not registered with ffscouter");
        }
        
        var userProfile = await client.GetUserProfileByDiscordId(Context.User.Id);
        var apiKey = new ApiKey(userProfile.Id, key, AccessLevel.FfScouter);

        try
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            var faction = await context.Factions
                .Include(faction => faction.ApiKeys)
                .SingleOrDefaultAsync(f => f.GuildId == Context.Guild!.Id);

            if (faction == null)
            {
                return MessageFactory.CreateErrorMessage<InteractionMessageProperties>("Faction not registered");
            }
            
            if (faction.ApiKeys.Any(k => k.Key == key))
                return MessageFactory.CreateErrorMessage<InteractionMessageProperties>("Key already registered");
            
            faction.ApiKeys.Add(apiKey);

            await context.SaveChangesAsync();
            
            return MessageFactory.CreateDefaultMessage<InteractionMessageProperties>("Success", "Your api key has been saved");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, $"Could not save ffscouter api key: {key}");
            return MessageFactory.CreateErrorMessage<InteractionMessageProperties>(
                "Something went wrong while saving your key");
        }
    }
}