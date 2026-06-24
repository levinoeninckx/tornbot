using System.Text.Json;
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

namespace TornBot.Bot.Features.Configurations;

[SlashCommand("configure", "Configure command", DefaultGuildPermissions = Permissions.Administrator)]
public class ConfigurationCommandModule(ChannelService channelService, TornApiClient client, TornbotContext context)
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
    
    [SubSlashCommand("bot", "configure this bot for your server")]
    public async Task<InteractionMessageProperties> ConfigureBot([SlashCommandParameter(Name = "key", Description = "Initial api key to register faction, can be public")] string apiKey)
    {
        if (Context.Guild == null)
        {
            return MessageFactory.CreateErrorMessage<InteractionMessageProperties>();
        }
        
        var isRegistered = await context.Factions.AnyAsync(f => f.GuildId == Context.Guild.Id);

        if (isRegistered)
        {
            return MessageFactory.CreateErrorMessage<InteractionMessageProperties>("Faction already registered");
        }
        
        var keyInfo = await client.GetKeyInfoAsync(apiKey);
        if (keyInfo == null) return MessageFactory.CreateErrorMessage<InteractionMessageProperties>("Invalid API key");

        var initialKey = new ApiKey(keyInfo.User.Id, apiKey, AccessLevel.Public);
        var faction = new Faction()
        {
            GuildId = Context.Guild.Id,
            FactionId = keyInfo.User.FactionId,
            ApiKeys = [initialKey],
            ModuleConfigs = [
                new ModuleConfig()
                {
                    Module = Module.Verification,
                    Config = JsonDocument.Parse(JsonSerializer.Serialize(new VerificationConfig()))
                }
            ]
        };
        
        context.Factions.Add(faction);
        await context.SaveChangesAsync();
        
        var factionBasic = await client.GetFactionBasicAsync(faction.FactionId);
        return MessageFactory.CreateDefaultMessage<InteractionMessageProperties>("Success", $"Faction {factionBasic.Name} registered");
    }

    [SubSlashCommand("verification", "configure verification")]
    public async Task<InteractionMessageProperties> ConfigureVerification()
    {
        var faction = await context.Factions
            .Include(f => f.ModuleConfigs)
            .SingleOrDefaultAsync(f => f.GuildId == Context.Guild!.Id);

        if (faction == null)
        {
            return MessageFactory.CreateErrorMessage<InteractionMessageProperties>("register faction first with /configure faction");
        }

        var config = faction.ModuleConfigs!.Single(c => c.Module == Module.Verification).Config.Deserialize<VerificationConfig>();
        return new()
        {
            Content = "Configure verification",
            Components =
            [
                new RoleMenuProperties("default_verification_roles")
                    .WithPlaceholder("Select default assigned roles")
                    .WithMaxValues(25)
                    .WithDefaultValues(config!.DefaultRoleIds),
                new ChannelMenuProperties("auto_verification_channel")
                    .WithPlaceholder("Select channel for verification messages for new users")
                    .WithMaxValues(1)
                    .WithDefaultValues([config.AutoVerificationChannelId])
            ],
            Flags = MessageFlags.Ephemeral
        };
    }
}
