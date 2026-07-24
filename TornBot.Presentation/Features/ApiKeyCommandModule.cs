using System.Text;
using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using TornBot.Application.Services;
using TornBot.Bot.Shared;
using TornBot.Domain.Enums;
using TornBot.Domain.Models;

namespace TornBot.Bot.Features;

[SlashCommand("key", "key commands")]
public class ApiKeyCommandModule(
    ApiKeyService apiKeyService,
    PlayerService playerService,
    ILogger<ApiKeyCommandModule> logger
) : ApplicationCommandModule<ApplicationCommandContext>
{
    [SubSlashCommand("add", "add an api key")]
    public async Task<InteractionMessageProperties> SetApiKey(
        [SlashCommandParameter(Description = "Your api key")] string key)
    {
        var player = await playerService.GetPlayerByDiscordId(Context.User.Id);
        if (player == null)
        {
            return MessageFactory.CreateErrorMessage<InteractionMessageProperties>(
                "Torn profile not found. User needs to register with the official torn discord");
        }

        var keyInfo = await client.GetKeyInfoAsync(key);

        var apiKey = new ApiKey(player.Id, key, AccessLevel.Public);

        if (keyInfo == null)
        {
            return MessageFactory.CreateErrorMessage<InteractionMessageProperties>("Invalid API key");
        }

        var faction = await context.Factions
            .Include(f => f.ApiKeys)
            .SingleOrDefaultAsync(f => f.GuildId == Context.Guild!.Id);
        if (faction == null)
        {
            return MessageFactory.CreateErrorMessage<InteractionMessageProperties>(
                "Please register this faction with /configure bot");
        }

        faction.ApiKeys.Add(apiKey);

        try
        {
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, $"Failed to save api key: {key}");
            return MessageFactory.CreateErrorMessage<InteractionMessageProperties>(
                "Please register this faction with /configure bot");
        }

        return new()
        {
            Flags = MessageFlags.Ephemeral,
            Content = $"{apiKey.AccessLevel} key: {apiKey.Key} added"
        };
    }

    [SubSlashCommand("remove", "remove an api key")]
    public async Task<InteractionMessageProperties> RemoveApiKey(
        [SlashCommandParameter(Description = "Your api key")]
        string key)
    {
        var apiKey = await apiKeyService.GetApiKeyAsync(key);

        if (apiKey == null)
        {
            // TODO: change to warning message
            return MessageFactory.CreateErrorMessage<InteractionMessageProperties>("API key not found");
        }

        var faction = await context.Factions
            .Include(f => f.ApiKeys)
            .SingleOrDefaultAsync(f => f.GuildId == Context.Guild!.Id);

        if (faction == null)
        {
            return MessageFactory.CreateErrorMessage<InteractionMessageProperties>(
                "Please register this faction with /configure bot");
        }

        if (faction.ApiKeys.Any(k => k.Key == key) || faction.ApiKeys.Count == 0)
        {
            return MessageFactory.CreateErrorMessage<InteractionMessageProperties>("API key not found");
        }

        faction.ApiKeys.Remove(apiKey);

        var message = MessageFactory.CreateEphermalMessage<InteractionMessageProperties>("Key removed",
            $"api key: {apiKey.Key} removed");

        return message;
    }

    [SubSlashCommand("list", "list all your api keys")]
    public async Task<InteractionMessageProperties> ListApiKeys(
        [SlashCommandParameter(Description = "show keys for specific user")] GuildUser? user = null)
    {
        var keys = await apiKeyService.GetAllApiKeysAsync();

        if (!keys.Any())
        {
            return MessageFactory.CreateEphermalMessage<InteractionMessageProperties>("No keys", "No api keys found");
        }

        var guildUser = user ?? Context.User as GuildUser;

        if (guildUser == null)
        {
            return MessageFactory.CreateErrorMessage<InteractionMessageProperties>();
        }

        var tornProfile = await client.GetUserProfileByDiscordId(guildUser.Id);

        var apiKeys = await context.ApiKeys.ToListAsync();

        if (!apiKeys.Any())
        {
            return new()
            {
                Flags = MessageFlags.Ephemeral,
                Content = $"No api keys found for this {tornProfile.Name}"
            };
        }

        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("");
        stringBuilder.AppendLine($"Api keys for {tornProfile.Name}");
        foreach (var apiKey in apiKeys)
        {
            stringBuilder.AppendLine($"{GetAccessLevelString(apiKey.AccessLevel)} key: {apiKey.Key}");
        }

        return new()
        {
            Flags = MessageFlags.Ephemeral,
            Embeds = [new EmbedProperties { Title = "Api keys", Description = stringBuilder.ToString() }]
        };
    }

    private static string GetAccessLevelString(AccessLevel accessLevel)
    {
        return accessLevel switch
        {
            AccessLevel.Public => "Public",
            AccessLevel.LimitedAccess => "Limited",
            AccessLevel.Minimal => "Minimal",
            AccessLevel.Full => "Full",
            _ => "Unknown"
        };
    }
}