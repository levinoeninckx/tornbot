using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetCord;
using NetCord.Services;
using NetCord.Services.ApplicationCommands;
using TornBot.Bot.Infrastructure;

namespace TornBot.Bot.Features.OrganizedCrime;

public class RequireOrganizedCrimesAllowedRoles : PreconditionAttribute<ApplicationCommandContext>
{
    public override async ValueTask<PreconditionResult> EnsureCanExecuteAsync(ApplicationCommandContext context,
        IServiceProvider? serviceProvider)
    {
        if (serviceProvider == null)
        {
            throw new ArgumentNullException(nameof(serviceProvider));
        }

        using var scope = serviceProvider.CreateScope();

        var configRepository = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TornbotContext>>();
        await using var dbContext = await configRepository.CreateDbContextAsync();
        var faction = await dbContext.Factions
            .Include(f => f.ModuleConfigs)
            .FirstOrDefaultAsync(f => f.GuildId == context.Guild!.Id);

        if (faction == null)
        {
            return PreconditionResult.Fail("Faction not registered");
        }

        var organizedCrimConfig = faction.OrganizedCrimeModuleConfig;

        if (organizedCrimConfig == null)
        {
            return PreconditionResult.Fail("Organized crime module is not configured for this guild.");
        }

        if (organizedCrimConfig.AllowedRoleIds.Count == 0)
        {
            return PreconditionResult.Success;
        }

        var userRoles = ((GuildUser)context.User).RoleIds;

        if (!userRoles.Any(r => organizedCrimConfig.AllowedRoleIds.Contains(r)))
        {
            return PreconditionResult.Fail("You are not allowed to use this command.");
        }

        return PreconditionResult.Success;
    }
}