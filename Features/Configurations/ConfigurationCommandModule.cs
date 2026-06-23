using discordBotTest.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using TornBot.Bot.Infrastructure;
using TornBot.Bot.Infrastructure.TornApi;
using TornBot.Bot.Shared;

namespace TornBot.Bot.Features.Configurations;

[SlashCommand("configure", "Configure command")]
public class ConfigurationCommandModule(ChannelService channelService, FactionService factionService, TornApiClient client, TornbotContext context)
    : ApplicationCommandModule<ApplicationCommandContext>
{
    [SubSlashCommand("chain", "configure this channel for chain monitoring")]
    public string SetChainMonitoringChannel()
    {
        if(Context.Channel == null) throw new InvalidOperationException();
        var channelId = Context.Channel.Id;

        channelService.AddChannelId(TrackingChannel.Chain, channelId);

        return "This channel is configured for chain monitoring";
    }

    [SubSlashCommand("war", "configure this channel for war monitoring")]
    public string SetWarMonitoringChannel()
    {
        if(Context.Channel == null) throw new InvalidOperationException();
        var channelId = Context.Channel.Id;

        channelService.AddChannelId(TrackingChannel.War, channelId);

        return "This channel is configured for war monitoring";
    }
    
    [SubSlashCommand("faction", "configure this bot for your faction")]
    public async Task<InteractionMessageProperties> SetFaction()
    {
        if (Context.Guild == null)
        {
            return MessageFactory.CreateErrorMessage<InteractionMessageProperties>();
        }

        var tonrProfile = await client.GetUserProfileByDiscordId(Context.User.Id);
        var userFaction = await client.GetUserFactionAsync(tonrProfile.Id);
        
        if (userFaction == null) return MessageFactory.CreateErrorMessage<InteractionMessageProperties>("You are not in a faction");
        
        var success = await factionService.AddFactionAsync(userFaction.Id, Context.Guild.Id);
        
        var faction = await client.GetFactionBasicAsync(userFaction.Id);
        
        var guildRoles = await Context.Guild.GetRolesAsync();

        if (guildRoles.All(r => r.Name != faction.Name))
        {
            var roleProperties = new RoleProperties()
            {
                Name = faction.Name,
            };
            await Context.Guild.CreateRoleAsync(roleProperties);
        }
        
        return success ? MessageFactory.CreateDefaultMessage<InteractionMessageProperties>("Success", "Faction registered") : MessageFactory.CreateErrorMessage<InteractionMessageProperties>();
    }

    [SubSlashCommand("verification", "configure verification")]
    public async Task<InteractionMessageProperties> ConfigureVerification()
    {
        var defaultRoles = await context.AuthRoles.Where(r => r.IsDefault).ToListAsync();
        return new()
        {
            Content = "Configure verification",
            Components =
            [
                new RoleMenuProperties("default_verification_roles")
                    .WithPlaceholder("Select default assigned roles")
                    .WithMaxValues(25)
                    .WithDefaultValues(defaultRoles.Select(r => r.RoleId).ToList())
            ],
            Flags = MessageFlags.Ephemeral
        };
    }
}
