using Microsoft.Extensions.DependencyInjection;
using NetCord.Services;
using NetCord.Services.ApplicationCommands;
using TornBot.Bot.Infrastructure;

namespace TornBot.Bot.Features.Banking;

public class RequireBankingChannel : PreconditionAttribute<ApplicationCommandContext>
{
    public override async ValueTask<PreconditionResult> EnsureCanExecuteAsync(ApplicationCommandContext context, IServiceProvider? serviceProvider)
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
            return PreconditionResult.Fail("Banking module is not configured for this guild.");
        }

        if (bankingConfig.RestrictedChannelIds.Count == 0)
        {
            return PreconditionResult.Success;
        }
        
        if (!bankingConfig.RestrictedChannelIds.Contains(context.Channel.Id))
        {
            return PreconditionResult.Fail("You are not allowed to use this command in this channel.");
        }
        
        return PreconditionResult.Success;
    }
}