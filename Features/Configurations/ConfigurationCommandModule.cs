using System.Text.Json;
using discordBotTest.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using TornBot.Bot.Domain.Enums;
using TornBot.Bot.Domain.Models;
using TornBot.Bot.Features.Banking;
using TornBot.Bot.Infrastructure;
using TornBot.Bot.Infrastructure.TornApi;
using TornBot.Bot.Shared;

namespace TornBot.Bot.Features.Configurations;

[SlashCommand("configure", "Configure command", DefaultGuildPermissions = Permissions.Administrator, Contexts = [InteractionContextType.Guild])]
public class ConfigurationCommandModule(ModuleConfigRepository moduleConfigRepository, TornApiClient client, TornbotContext context, ILogger<ConfigurationCommandModule> logger)
    : ApplicationCommandModule<ApplicationCommandContext>
{
    [SubSlashCommand("bot", "configure this bot for your server")]
    public async Task<InteractionMessageProperties> ConfigureBot([SlashCommandParameter(Name = "key", Description = "Initial api key to register faction, can be public")] string apiKey)
    {
        if (Context.Guild == null)
        {
            logger.LogWarning("Guild is null");
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

        try
        {
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Failed to save faction");
            return MessageFactory.CreateErrorMessage<InteractionMessageProperties>("Failed to save faction");
        }
        
        var factionBasic = await client.GetFactionBasicAsync(faction.FactionId);
        return MessageFactory.CreateDefaultMessage<InteractionMessageProperties>("Success", $"Faction {factionBasic.Name} registered");
    }

    [SubSlashCommand("verification", "configure the verification module")]
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
            Components =
            [
                new ComponentContainerProperties()
                {
                    new TextDisplayProperties("Default roles"),
                    new RoleMenuProperties("default_verification_roles")
                        .WithPlaceholder("Select default assigned roles")
                        .WithMinValues(0)
                        .WithMaxValues(25)
                        .WithDefaultValues(config!.DefaultRoleIds),
                    new TextDisplayProperties("Faction roles"),
                    new RoleMenuProperties("verification_faction_roles")
                        .WithPlaceholder("Select roles assigned to faction members")
                        .WithMinValues(0)
                        .WithMaxValues(25)
                        .WithDefaultValues(config.FactionRoleIds),
                    new TextDisplayProperties("Non faction roles"),
                    new RoleMenuProperties("verification_non_faction_roles")
                        .WithPlaceholder("Select roles assigned to non-faction members")
                        .WithMinValues(0)
                        .WithMaxValues(25)
                        .WithDefaultValues(config.NonFactionRoleIds),
                    new TextDisplayProperties("Allowed roles"),
                    new RoleMenuProperties("verification_allowed_roles")
                        .WithPlaceholder("Select roles allowed to use verify commands")
                        .WithMinValues(0)
                        .WithMaxValues(25)
                        .WithDefaultValues(config.AllowedRoleIds),
                    new TextDisplayProperties("Restricted channels"),
                    new ChannelMenuProperties("restricted_channels")
                        .WithPlaceholder("Confine commands to these channels")
                        .WithMinValues(0)
                        .WithMaxValues(25)
                        .WithDefaultValues(config.RestrictedChannelIds),
                    new TextDisplayProperties("Auto verification channel"),
                    new ChannelMenuProperties("auto_verification_channel")
                        .WithPlaceholder("Select channel for verification messages for new users")
                        .WithMinValues(0)
                        .WithMaxValues(1)
                        .WithDefaultValues([config.AutoVerificationChannelId])
                }
            ],
            Flags = MessageFlags.Ephemeral | MessageFlags.IsComponentsV2
        };
    }

    [SubSlashCommand("banking", "configure the banking module")]
    public async Task<InteractionMessageProperties> ConfigureBanking()
    {
        var bankingConfig = await moduleConfigRepository.GetBankingModuleConfigByGuildId(Context.Guild!.Id);

        if (bankingConfig == null)
        {
            return MessageFactory.CreateEphermalMessage<InteractionMessageProperties>("Oops","Could not get banking module config");
        }

        return new()
        {
            Components =
            [
                new ComponentContainerProperties()
                {
                    new TextDisplayProperties("Enable/disable banking"),
                    new StringMenuProperties("banking_enabled")
                        .WithOptions([
                            new StringMenuSelectOptionProperties("Enabled", nameof(ModuleState.Enabled)) { Default = bankingConfig.State == ModuleState.Enabled},
                            new StringMenuSelectOptionProperties("Disabled", nameof(ModuleState.Disabled)) { Default = bankingConfig.State == ModuleState.Disabled}
                        ])
                        .WithMinValues(1)
                        .WithMaxValues(1),
                    new TextDisplayProperties("Banker role"),
                    new RoleMenuProperties("banker_roles")
                        .WithPlaceholder("Select role for bankers")
                        .WithMinValues(0)
                        .WithMaxValues(1)
                        .WithDefaultValues(bankingConfig.BankerRoleId.HasValue ? [bankingConfig.BankerRoleId!.Value] : []),
                    new TextDisplayProperties("Restricted channels"),
                    new ChannelMenuProperties("banking_restricted_channels")
                        .WithPlaceholder("Select channel for banking messages")
                        .WithMinValues(0)
                        .WithMaxValues(1)
                        .WithDefaultValues(bankingConfig.RestrictedChannelIds),
                    new TextDisplayProperties("Allowed roles"),
                    new RoleMenuProperties("banking_allowed_roles")
                        .WithPlaceholder("Select role for banking")
                        .WithMinValues(0)
                        .WithMaxValues(25)
                        .WithDefaultValues(bankingConfig.AllowedRoleIds)
                }
            ],
            Flags = MessageFlags.Ephemeral | MessageFlags.IsComponentsV2
        };
    }
    
}
