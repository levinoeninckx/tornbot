using discordBotTest.Shared;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using TornBot.Bot.Infrastructure.TornApi;
using TornBot.Bot.Shared;

namespace TornBot.Bot.Features;

[SlashCommand("key", "key commands")]
public class ApiKeyCommandModule(ApiKeyService apiKeyService, TornApiClient client) : ApplicationCommandModule<ApplicationCommandContext>
{
    [SubSlashCommand("set", "set the api key")]
    public async Task<InteractionMessageProperties> SetApiKey([SlashCommandParameter(Description = "Your api key")]string key)
    {
        var validKey = await client.ValidateKeyAsync(key);

        if (!validKey)
        {
            return new()
            {
                Flags = MessageFlags.Ephemeral,
                Content = "Invalid API key"
            };
        }
        
        apiKeyService.SetApiKey(key);
        return new()
        {
            Flags = MessageFlags.Ephemeral,
            Content = $"API key: {key} set"
        };
    }   
}