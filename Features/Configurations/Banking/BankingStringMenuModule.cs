using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;
using TornBot.Bot.Domain.Enums;
using TornBot.Bot.Infrastructure;
using TornBot.Bot.Shared;

namespace TornBot.Bot.Features.Configurations.Banking;

public class BankingStringMenuModule(IDbContextFactory<TornbotContext> dbContextFactory) : ComponentInteractionModule<StringMenuInteractionContext>
{
    [ComponentInteraction("banking_enabled")]
    public async Task ToggleBankingAllowed()
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

        var state = Enum.Parse<ModuleState>(Context.SelectedValues.Single());

        config!.State = state;

        var jsonDoc = JsonDocument.Parse(JsonSerializer.Serialize(config));

        await dbContext.Factions.UpdateModuleConfig(faction.GuildId, Module.Banking, jsonDoc);
        await dbContext.SaveChangesAsync();

        await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredModifyMessage);
    }
}