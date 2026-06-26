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
        var repository = scope.ServiceProvider.GetRequiredService<ModuleConfigRepository>();

        var bankingConfig = await repository.GetBankingModuleConfigByGuildId(context.Guild!.Id);
        
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