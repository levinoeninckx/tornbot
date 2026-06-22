using System.Text;
using discordBotTest.Shared;
using Microsoft.EntityFrameworkCore;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using TornBot.Bot.Domain.Enums;
using TornBot.Bot.Domain.Models;
using TornBot.Bot.Infrastructure;
using TornBot.Bot.Infrastructure.TornApi;
using TornBot.Bot.Shared;

namespace TornBot.Bot.Features;

[SlashCommand("key", "key commands")]
public class ApiKeyCommandModule(ApiKeyService apiKeyService, TornApiClient client) : ApplicationCommandModule<ApplicationCommandContext>
{
    [SubSlashCommand("add", "add an api key")]
    public async Task<InteractionMessageProperties> SetApiKey([SlashCommandParameter(Description = "Your api key")]string key)
    {
        var existingKeys = await apiKeyService.GetAllApiKeysAsync();
        
        if (existingKeys.Any(k => k.Key == key))
        {
            // TODO: change to warning message
            var message = MessageFactory.CreateErrorMessage<InteractionMessageProperties>("API key is already added");
            
            message.Flags = MessageFlags.Ephemeral;

            return message;
        }
        
        var keyInfo = await client.GetKeyInfoAsync(key);

        if (keyInfo == null)
        {
            return MessageFactory.CreateErrorMessage<InteractionMessageProperties>("Invalid API key");
        }
        
        var apiKey = new ApiKey(keyInfo.User.Id, key, (AccessLevel)keyInfo.Access.Level);
        
        await apiKeyService.AddKeyAsync(apiKey);
        
        return new()
        {
            Flags = MessageFlags.Ephemeral,
            Content = $"{apiKey.AccessLevel} key: {apiKey.Key} added"
        };
    }

    [SubSlashCommand("remove", "remove an api key")]
    public async Task<InteractionMessageProperties> RemoveApiKey(
        [SlashCommandParameter(Description = "Your api key")] string key)
    {
        var apiKey = await apiKeyService.GetApiKeyAsync(key);

        if (apiKey == null)
        {
            // TODO: change to warning message
            return MessageFactory.CreateErrorMessage<InteractionMessageProperties>("API key not found");
        }
        
        var isDeleted = await apiKeyService.RemoveKeyAsync(apiKey);

        if (!isDeleted)
        {
            return MessageFactory.CreateErrorMessage<InteractionMessageProperties>("Failed to remove API key");
        }

        // TODO: add createEphermal message
        var message = MessageFactory.CreateDefaultMessage<InteractionMessageProperties>("Key removed",
            $"api key: {apiKey.Key} removed");
        
        message.Flags = MessageFlags.Ephemeral;

        return message;
    }

    [SubSlashCommand("list", "list all your api keys")]
    public async Task<InteractionMessageProperties> ListApiKeys([SlashCommandParameter(Description = "show keys for specific user")] GuildUser? user = null)
    {
        var guildUser = user ?? Context.User as GuildUser;

        if (guildUser == null)
        {
            return MessageFactory.CreateErrorMessage<InteractionMessageProperties>();
        }
        
        var tornProfile = await client.GetUserProfileByDiscordId(guildUser.Id);
        
        var apiKeys = await apiKeyService.GetApiKeysByUserIdAsync(tornProfile.Id);

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
            Embeds = [ new EmbedProperties { Title = "Api keys", Description = stringBuilder.ToString() }]
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