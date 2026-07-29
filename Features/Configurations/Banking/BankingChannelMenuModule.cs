using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;
using TornBot.Bot.Domain.Enums;
using TornBot.Bot.Infrastructure;
using TornBot.Bot.Shared;

namespace TornBot.Bot.Features.Configurations.Banking;

public class BankingChannelMenuModule(IDbContextFactory<TornbotContext> dbContextFactory) : ComponentInteractionModule<ChannelMenuInteractionContext>
{
    [ComponentInteraction("banking_restricted_channels")]
    public async Task SetBankingChannels()
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
        }

        config!.RestrictedChannelIds = Context.SelectedValues.Select(x => x.Id).ToHashSet();
        var jsonDoc = JsonDocument.Parse(JsonSerializer.Serialize(config));
        await dbContext.Factions.UpdateModuleConfig(Context.Guild!.Id, Module.Banking, jsonDoc);
        
        await dbContext.SaveChangesAsync();
        
        await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredModifyMessage);
    }
}