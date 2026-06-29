using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetCord.Services;
using NetCord.Services.ApplicationCommands;
using TornBot.Bot.Domain.Enums;
using TornBot.Bot.Infrastructure;

namespace TornBot.Bot.Shared;

public class RequireKey(AccessLevel accessLevel, bool needsFactionAccess) : PreconditionAttribute<ApplicationCommandContext>
{
    public override async ValueTask<PreconditionResult> EnsureCanExecuteAsync(ApplicationCommandContext context, IServiceProvider? serviceProvider)
    {
        if (serviceProvider == null)
        {
            throw new ArgumentNullException(nameof(serviceProvider));
        }

        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TornbotContext>();

        var queryable = dbContext.Factions
            .Include(f => f.ApiKeys)
            .Where(f => context.Guild!.Id == f.GuildId)
            .SelectMany(f => f.ApiKeys);
        
        var hasRequiredKey = needsFactionAccess ? await queryable.AnyAsync(k => k.AccessLevel == accessLevel && k.HasFactionAccess == needsFactionAccess) : await queryable.AnyAsync(k => k.AccessLevel == accessLevel);

        if (needsFactionAccess && !hasRequiredKey)
        {
            return PreconditionResult.Fail($"Register a key with access level {accessLevel} and faction API access");
        }
        
        return hasRequiredKey ? PreconditionResult.Success : PreconditionResult.Fail($"Register a key with access level {accessLevel}");
    }
}