using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;
using TornBot.Bot.Shared;
using TornBot.Infrastructure.Persistence;

namespace TornBot.Bot.Features.Configurations.Verification;

public class VerificationRoleMenuModule(TornbotContext context, ILogger<VerificationRoleMenuModule> logger)
    : ComponentInteractionModule<RoleMenuInteractionContext>
{
    [ComponentInteraction("default_verification_roles")]
    public Task SetDefaultVerificationRoles() => UpdateVerificationConfigAsync(config =>
        config.DefaultRoleIds = [.. Context.SelectedValues.Select(r => r.Id)]);

    [ComponentInteraction("verification_faction_roles")]
    public Task SetFactionRoles() => UpdateVerificationConfigAsync(config => config.FactionRoleIds =
        [.. Context.SelectedValues.Select(r => r.Id)]);

    [ComponentInteraction("verification_non_faction_roles")]
    public Task SetNonFactionRoles() => UpdateVerificationConfigAsync(config => config.NonFactionRoleIds =
        [.. Context.SelectedValues.Select(r => r.Id)]);

    [ComponentInteraction("verification_required_roles")]
    public Task SetAllowedRoles() => UpdateVerificationConfigAsync(config => config.AllowedRoleIds =
        [.. Context.SelectedValues.Select(r => r.Id)]);

    private async Task UpdateVerificationConfigAsync(Action<VerificationConfig> updateAction)
    {
        if (Context.Guild == null) return;

        var faction = await context.Factions
            .Include(f => f.ModuleConfigs)
            .SingleOrDefaultAsync(f => f.GuildId == Context.Guild.Id);

        if (faction == null)
        {
            await Context.Interaction.SendResponseAsync(InteractionCallback.Message(
                MessageFactory.CreateErrorMessage<InteractionMessageProperties>(
                    "Please register this faction with /configure bot")));
            return;
        }

        var moduleConfig = faction.ModuleConfigs.SingleOrDefault(c => c.Module == Module.Verification);
        var config = moduleConfig?.Config.Deserialize<VerificationConfig>();

        if (config == null || moduleConfig == null) return;

        updateAction(config);
        moduleConfig.Config = JsonDocument.Parse(JsonSerializer.Serialize(config));

        try
        {
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Failed to save verification roles");
            var msg = MessageFactory.CreateErrorMessage<InteractionMessageProperties>(
                "Failed to save verification roles");
            await Context.Interaction.SendFollowupMessageAsync(msg);
            await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredModifyMessage);
            return;
        }

        await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredModifyMessage);
    }
}