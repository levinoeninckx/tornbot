using System.Text.Json;
using discordBotTest.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using Quartz;
using TornBot.Bot.Domain.Enums;
using TornBot.Bot.Domain.Models;
using TornBot.Bot.Infrastructure;
using TornBot.Bot.Infrastructure.TornApi;
using TornBot.Bot.Shared;

namespace TornBot.Bot.Features.Configurations;

[SlashCommand("configure", "Configure command", DefaultGuildPermissions = Permissions.Administrator, Contexts = [InteractionContextType.Guild])]
public class ConfigurationCommandModule(
    ModuleConfigRepository moduleConfigRepository, 
    TornApiClient client, 
    IDbContextFactory<TornbotContext> contextFactory, 
    ISchedulerFactory schedulerFactory,
    ILogger<ConfigurationCommandModule> logger
    )
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
        
        await using var context = await contextFactory.CreateDbContextAsync();
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

        await SetOcTriggersAsync();
        
        var factionBasic = await client.GetFactionBasicAsync(faction.FactionId);
        if(factionBasic == null) return MessageFactory.CreateErrorMessage<InteractionMessageProperties>("Failed to get faction information");
        
        return MessageFactory.CreateDefaultMessage<InteractionMessageProperties>("Success", $"Faction {factionBasic.Name} registered");
    }

    [SubSlashCommand("verification", "configure the verification module")]
    public async Task<InteractionMessageProperties> ConfigureVerification()
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var faction = await context.Factions
            .Include(f => f.ModuleConfigs)
            .SingleOrDefaultAsync(f => f.GuildId == Context.Guild!.Id);

        if (faction == null)
        {
            return MessageFactory.CreateErrorMessage<InteractionMessageProperties>("register faction first with /configure faction");
        }

        var config = faction.ModuleConfigs!.Single(c => c.Module == Module.Verification).Config.Deserialize<VerificationConfig>();

        if (config == null)
        {
            return MessageFactory.CreateErrorMessage<InteractionMessageProperties>("Module configuration not found");
        }

        return new ConfigurationMenuBuilder()
            .AddEnableModuleMenu("verification_enabled", config.Enabled)
            .AddRequiredRolesMenu("verification_required_roles", config.AllowedRoleIds)
            .AddRestrictedChannelsMenu("verification_restricted_channels", config.RestrictedChannelIds)
            .Build()
            .AddComponents(                    
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
                new TextDisplayProperties("Auto verification channel"),
                new ChannelMenuProperties("auto_verification_channel")
                    .WithPlaceholder("Select channel for verification messages for new users")
                    .WithMinValues(0)
                    .WithMaxValues(1)
                    .WithDefaultValues([config.AutoVerificationChannelId])
            );
    }

    [SubSlashCommand("banking", "configure the banking module")]
    public async Task<InteractionMessageProperties> ConfigureBanking()
    {
        var bankingConfig = await moduleConfigRepository.GetBankingModuleConfigByGuildId(Context.Guild!.Id);

        if (bankingConfig == null)
        {
            return MessageFactory.CreateEphermalMessage<InteractionMessageProperties>("Oops","Could not get banking module config");
        }

        return new ConfigurationMenuBuilder()
            .AddEnableModuleMenu("banking_enabled", bankingConfig.State)
            .AddRequiredRolesMenu("banking_allowed_roles", bankingConfig.AllowedRoleIds)
            .AddRestrictedChannelsMenu("banking_restricted_channels", bankingConfig.RestrictedChannelIds)
            .Build()
            .AddComponents(
                new TextDisplayProperties("Banker role"),
                new RoleMenuProperties("banker_roles")
                    .WithPlaceholder("Select role for bankers")
                    .WithMinValues(0)
                    .WithMaxValues(1)
                    .WithDefaultValues(bankingConfig.BankerRoleId.HasValue ? [bankingConfig.BankerRoleId!.Value] : [])
                );
    }

    [SubSlashCommand("oc", "configure the OC module")]
    public async Task<InteractionMessageProperties> ConfigureOc()
    {
        var ocConfig = await moduleConfigRepository.GetOrganizedCrimeModuleConfigByGuildId(Context.Guild!.Id);
        if (ocConfig == null)
        {
            return MessageFactory.CreateEphermalMessage<InteractionMessageProperties>("Oops","Could not get OC module config");
        }
        
        return new ConfigurationMenuBuilder()
            .AddEnableModuleMenu("oc_enabled", ocConfig.State)
            .AddRequiredRolesMenu("oc_allowed_roles", ocConfig.AllowedRoleIds)
            .AddRestrictedChannelsMenu("oc_restricted_channels", ocConfig.RestrictedChannelIds)
            .Build()
            .AddComponents(
                new TextDisplayProperties("Enable OC notifications"),
                new StringMenuProperties("oc_notifications_enabled")
                    .WithOptions([
                        new StringMenuSelectOptionProperties("Enabled", nameof(ModuleState.Enabled))
                            { Default = ocConfig.NotificationState == ModuleState.Enabled },
                        new StringMenuSelectOptionProperties("Disabled", nameof(ModuleState.Disabled))
                            { Default = ocConfig.NotificationState == ModuleState.Disabled }
                    ])
                    .WithMinValues(1)
                    .WithMaxValues(1),
                new TextDisplayProperties("OC notification role"),
                new RoleMenuProperties("oc_notification_role")
                    .WithPlaceholder("Select role for OC notifications")
                    .WithMinValues(0)
                    .WithMaxValues(1)
                    .WithDefaultValues(ocConfig.NotificationRoleId.HasValue ? [ocConfig.NotificationRoleId!.Value] : null),
                new TextDisplayProperties("OC notification channel"),
                new ChannelMenuProperties("oc_notification_channel")
                    .WithPlaceholder("Select channel for OC notifications")
                    .WithMinValues(0)
                    .WithMaxValues(1)
                    .WithDefaultValues(ocConfig.NotificationChannelId.HasValue ? [ocConfig.NotificationChannelId!.Value] : null));
    }

    [SubSlashCommand("notifications", "set up the background tasks for notifications")]
    public async Task<InteractionMessageProperties> ConfigureNotifications()
    {
        await SetOcTriggersAsync();
        return MessageFactory.CreateDefaultMessage<InteractionMessageProperties>("Success", "Background tasks set up");
    }
    
    [SubSlashCommand("retal", "configure the retailed module")]
    public async Task<InteractionMessageProperties> ConfigureRetail()
    {
        await SetRetalTriggersAsync();
        return MessageFactory.CreateDefaultMessage<InteractionMessageProperties>("Success", "Retail module configured");
    }

    private async Task SetRetalTriggersAsync()
    {
        var scheduler = await schedulerFactory.GetScheduler();

        var trigger = await scheduler.GetTrigger(new TriggerKey($"retal-trigger-{Context.Guild!.Id}"));
        if (trigger != null)
            return;
    }

    private async Task SetOcTriggersAsync()
    {
        var scheduler = await schedulerFactory.GetScheduler();

        var trigger = await scheduler.GetTrigger(new TriggerKey($"oc-trigger-{Context.Guild!.Id}"));
        if (trigger != null)
            return;
        
        var ocTrigger = TriggerBuilder.Create()
            .WithIdentity($"oc-trigger-{Context.Guild!.Id}")
            .WithSimpleSchedule(x => x.WithIntervalInSeconds(30).RepeatForever())
            .StartAt(DateTimeOffset.UtcNow.AddSeconds(20))
            .ForJob(new JobKey("GetNewCrimes", "OC"))
            .UsingJobData("guildId", Context.Guild!.Id.ToString())
            .Build();
        
        await scheduler.ScheduleJob(ocTrigger);
    }
}
