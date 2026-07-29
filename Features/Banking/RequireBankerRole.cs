using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetCord;
using NetCord.Rest;
using NetCord.Services;
using NetCord.Services.ApplicationCommands;
using NetCord.Services.ComponentInteractions;
using TornBot.Bot.Infrastructure;
using TornBot.Bot.Shared;

namespace TornBot.Bot.Features.Banking;

public class RequireBankerRole : PreconditionAttribute<ButtonInteractionContext>
{
    public override async ValueTask<PreconditionResult> EnsureCanExecuteAsync(ButtonInteractionContext context, IServiceProvider? serviceProvider)
    {
        if (serviceProvider == null)
        {
            return PreconditionResult.Fail("Service provider is null.");
        }

        using var scope = serviceProvider.CreateScope();
        var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TornbotContext>>();
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var faction = await dbContext.Factions.GetFactionByGuildIdAsync(context.Guild!.Id, includeModuleConfigs: true);
        if (faction == null)
        {
            return PreconditionResult.Fail("Faction not found.");
        }

        var bankingConfig = faction.BankingModuleConfig;
        if (bankingConfig == null)
        {
            return PreconditionResult.Fail("Banking module config not found.");
        }

        if (bankingConfig.BankerRoleId == 0 || bankingConfig.BankerRoleId == null)
        {
            return PreconditionResult.Fail("Banker role is not configured.");
        }
        
        if (!((GuildUser)context.User).RoleIds.Contains(bankingConfig.BankerRoleId.Value))
        {
            var msg = MessageFactory.CreateEphermalMessage<InteractionMessageProperties>("Unauthorized", "You are not a banker.");
            await context.Interaction.SendResponseAsync(InteractionCallback.Message(msg));
            return PreconditionResult.Fail(string.Empty);
        }
        
        return PreconditionResult.Success;
    }
}