using Microsoft.Extensions.DependencyInjection;
using NetCord.Services;
using NetCord.Services.ApplicationCommands;
using TornBot.Infrastructure.Persistence;

namespace TornBot.Bot.Features.OrganizedCrime;

public class RequireOrganizedCrimeRestrictedChannels : PreconditionAttribute<ApplicationCommandContext>
{
    public override async ValueTask<PreconditionResult> EnsureCanExecuteAsync(ApplicationCommandContext context,
        IServiceProvider? serviceProvider)
    {
        if (serviceProvider == null)
        {
            return PreconditionResult.Fail("Service provider is null.");
        }

        using var scope = serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ModuleConfigRepository>();

        var organizedCrimeConfig = await repository.GetOrganizedCrimeModuleConfigByGuildId(context.Guild!.Id);
        if (organizedCrimeConfig == null)
        {
            return PreconditionResult.Fail("Organized crime module is not configured for this guild.");
        }

        if (organizedCrimeConfig.RestrictedChannelIds.Count == 0)
        {
            return PreconditionResult.Success;
        }

        if (!organizedCrimeConfig.RestrictedChannelIds.Contains(context.Channel.Id))
        {
            return PreconditionResult.Fail("You are not allowed to use this command in this channel.");
        }

        return PreconditionResult.Success;
    }
}