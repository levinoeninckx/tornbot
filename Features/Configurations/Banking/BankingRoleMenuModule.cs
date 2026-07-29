using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;
using TornBot.Bot.Domain.Enums;
using TornBot.Bot.Domain.Models;
using TornBot.Bot.Infrastructure;
using TornBot.Bot.Shared;

namespace TornBot.Bot.Features.Configurations.Banking;

public class BankingRoleMenuModule(IDbContextFactory<TornbotContext> dbContextFactory) : ComponentInteractionModule<RoleMenuInteractionContext>
{
    [ComponentInteraction("banker_roles")]
    public async Task SetBankerRoles()
    {
        await UpdateBankingConfigAsync(config =>
        {
            config.BankerRoleId = Context.SelectedValues.Single().Id;
        });
    }

    [ComponentInteraction("banking_allowed_roles")]
    public async Task SetAllowedRoles()
    {
        await UpdateBankingConfigAsync(config =>
        {
            config.AllowedRoleIds = Context.SelectedValues.Select(x => x.Id).ToHashSet();
        });
    }

    private async Task UpdateBankingConfigAsync(Action<BankingModuleConfig> updateAction)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var faction = await dbContext.Factions.GetFactionByGuildIdAsync(Context.Guild!.Id, includeModuleConfigs: true);
        if (faction == null)
        {
            var msg = MessageFactory.CreateErrorMessage<InteractionMessageProperties>("Faction not found");
            await Context.Interaction.SendResponseAsync(InteractionCallback.Message(msg));
            return;
        }

        var config = faction.BankingModuleConfig;
        if (config == null)
        {
            var msg = MessageFactory.CreateErrorMessage<InteractionMessageProperties>("Module configuration not found");
            await Context.Interaction.SendResponseAsync(InteractionCallback.Message(msg));
            return;
        }

        updateAction(config);

        var jsonDoc = JsonDocument.Parse(JsonSerializer.Serialize(config));
        await dbContext.Factions.UpdateModuleConfig(faction.GuildId, Module.Banking, jsonDoc);
        await dbContext.SaveChangesAsync();

        await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredModifyMessage);
    }
}