using Microsoft.Extensions.DependencyInjection;
using NetCord.Services;
using NetCord.Services.ApplicationCommands;
using TornBot.Infrastructure.Persistence;

namespace TornBot.Bot.Shared;

public class RequireModuleEnabled(Module module) : PreconditionAttribute<ApplicationCommandContext>
{
    public override async ValueTask<PreconditionResult> EnsureCanExecuteAsync(ApplicationCommandContext context,
        IServiceProvider? serviceProvider)
    {
        if (serviceProvider == null)
        {
            throw new ArgumentNullException(nameof(serviceProvider));
        }

        using var scope = serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ModuleConfigRepository>();
        switch (module)
        {
            case Module.Banking:
                var bankingConfig = await repo.GetBankingModuleConfigByGuildId(context.Guild!.Id);
                if (bankingConfig == null)
                {
                    return PreconditionResult.Fail("Module configuration not found");
                }

                if (bankingConfig.State == ModuleState.Disabled)
                {
                    return PreconditionResult.Fail("Banking module is disabled");
                }

                break;
            case Module.Verification:
                var config = await repo.GetBankingModuleConfigByGuildId(context.Guild!.Id);
                if (config == null)
                {
                    return PreconditionResult.Fail("Module configuration not found");
                }

                if (config.State == ModuleState.Disabled)
                {
                    return PreconditionResult.Fail("Banking module is disabled");
                }

                break;
        }

        return PreconditionResult.Success;
    }
}