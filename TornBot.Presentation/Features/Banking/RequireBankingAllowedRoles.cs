using Microsoft.Extensions.DependencyInjection;
using NetCord;
using NetCord.Services;
using NetCord.Services.ApplicationCommands;
using TornBot.Infrastructure.Persistence;

namespace TornBot.Bot.Features.Banking;

public class RequireBankingAllowedRoles : PreconditionAttribute<ApplicationCommandContext>
{
    public override async ValueTask<PreconditionResult> EnsureCanExecuteAsync(ApplicationCommandContext context,
        IServiceProvider? serviceProvider)
    {
        if (serviceProvider == null)
        {
            throw new ArgumentNullException(nameof(serviceProvider));
        }

        using var scope = serviceProvider.CreateScope();

        var configRepository = scope.ServiceProvider.GetRequiredService<ModuleConfigRepository>();

        var bankingConfig = await configRepository.GetBankingModuleConfigByGuildId(context.Guild!.Id);

        if (bankingConfig == null)
        {
            return PreconditionResult.Fail("Banking module is not configured for this guild.");
        }

        if (bankingConfig.AllowedRoleIds.Count == 0)
        {
            return PreconditionResult.Success;
        }

        var userRoles = ((GuildUser)context.User).RoleIds;

        if (!userRoles.Any(r => bankingConfig.AllowedRoleIds.Contains(r)))
        {
            return PreconditionResult.Fail("You are not allowed to use this command.");
        }

        return PreconditionResult.Success;
    }
}