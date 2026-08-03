using Microsoft.EntityFrameworkCore;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using TornBot.Bot.Domain.Enums;
using TornBot.Bot.Domain.Models;
using TornBot.Bot.Features.Retaliation.Models;
using TornBot.Bot.Infrastructure;
using TornBot.Bot.Infrastructure.TornApi;
using TornBot.Bot.Infrastructure.TornStats;
using TornBot.Bot.Shared;

namespace TornBot.Bot.Features;

[SlashCommand("ts", "torn stats commands", DefaultGuildPermissions = Permissions.Administrator)]
public class TornStatsCommandModule(TornApiClient client, TornStatClient tsClient, IDbContextFactory<TornbotContext> dbContextFactory) : ApplicationCommandModule<ApplicationCommandContext>
{
    [SubSlashCommand("key", "set the tornstats api key")]
    public async Task<InteractionMessageProperties> SetApiKey([SlashCommandParameter] string key)
    {
        var isKeyValid = await tsClient.IsKeyValidAsync(key);
        if (!isKeyValid)
        {
            return MessageFactory.CreateErrorMessage<InteractionMessageProperties>("Not a valid tornstats key");
        }
        
        var player = await client.GetUserProfileByDiscordId(Context.User.Id);
        var apiKey = new ApiKey(player.Id, key, AccessLevel.TornStats);
        
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var faction = await dbContext.Factions
            .Include(f => f.ApiKeys)
            .SingleOrDefaultAsync(f => f.GuildId == Context.Guild!.Id);

        if (faction == null)
            return MessageFactory.CreateErrorMessage<InteractionMessageProperties>("This guild is not registered");
        
        faction.ApiKeys.Add(apiKey);

        await dbContext.SaveChangesAsync();
        
        return MessageFactory.CreateDefaultMessage<InteractionMessageProperties>("Success", "Your api key has been saved");
    }

    [SubSlashCommand("stats", "get spy stats for a player")]
    public async Task<InteractionMessageProperties> GetStats([SlashCommandParameter] int playerId)
    {
        var stats = await tsClient.GetSpyProfileDetailsById(playerId);
        if (stats is null)
        {
            return MessageFactory.CreateDefaultMessage<InteractionMessageProperties>("No stats found",
                "No stats found for this player");
        }
        
        var battleStat = new BattleStat(
            Convert.ToUInt64(stats.Spy.Strength), 
            Convert.ToUInt64(stats.Spy.Defense), 
            Convert.ToUInt64(stats.Spy.Speed), 
            Convert.ToUInt64(stats.Spy.Dexterity)
        );

        return MessageFactory.CreateDefaultMessage<InteractionMessageProperties>("Player stats", battleStat.ToString());
    }
}