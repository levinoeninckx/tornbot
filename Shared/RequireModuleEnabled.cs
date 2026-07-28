using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetCord.Services;
using NetCord.Services.ApplicationCommands;
using TornBot.Bot.Domain.Enums;
using TornBot.Bot.Infrastructure;

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
        var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TornbotContext>>();
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var faction = await dbContext.Factions
            .Include(f => f.ModuleConfigs)
            .FirstOrDefaultAsync(f => f.GuildId == context.Guild!.Id);

        if (faction == null)
        {
            return PreconditionResult.Fail("Faction not registered");
        }

        switch (module)
        {
            case Module.Banking:
                var bankingConfig = faction.BankingModuleConfig;
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
                var verificationConfig = faction.VerificationModuleConfig;
                if (verificationConfig == null)
                {
                    return PreconditionResult.Fail("Module configuration not found");
                }

                if (verificationConfig.Enabled == ModuleState.Disabled)
                {
                    return PreconditionResult.Fail("Banking module is disabled");
                }

                break;
            case Module.OrganizedCrime:
                var organizedCrimeConfig = faction.VerificationModuleConfig;
                if (organizedCrimeConfig == null)
                {
                    return PreconditionResult.Fail("Module configuration not found");
                }

                if (organizedCrimeConfig.Enabled == ModuleState.Disabled)
                {
                    return PreconditionResult.Fail("Banking module is disabled");
                }

                break;
        }

        return PreconditionResult.Success;
    }
}