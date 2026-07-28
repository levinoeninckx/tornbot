using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetCord;
using NetCord.Rest;
using NetCord.Services;
using NetCord.Services.ApplicationCommands;
using TornBot.Bot.Domain.Enums;
using TornBot.Bot.Domain.Models;
using TornBot.Bot.Infrastructure;
using TornBot.Bot.Shared;

namespace TornBot.Bot.Features.Verification;

public class RequireVerificationChannelsAttribute : PreconditionAttribute<ApplicationCommandContext>
{
    public override async ValueTask<PreconditionResult> EnsureCanExecuteAsync(ApplicationCommandContext context,
        IServiceProvider? serviceProvider)
    {
        if (context.Guild == null || context.User is not GuildUser guildUser)
        {
            await context.Interaction.SendResponseAsync(InteractionCallback.Message(
                MessageFactory.CreateErrorMessage<InteractionMessageProperties>(
                    "You can't run this command outside a server")));
            return PreconditionResult.Fail(string.Empty);
        }

        // Bypass for owner or administrators if desired
        if (guildUser.Id == context.Guild.OwnerId)
            return PreconditionResult.Success;

        if (serviceProvider == null)
        {
            throw new ArgumentNullException(nameof(serviceProvider));
        }

        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TornbotContext>();

        var faction = await dbContext.Factions
            .Include(f => f.ModuleConfigs)
            .SingleOrDefaultAsync(f => f.GuildId == context.Guild.Id);

        var moduleConfig = faction?.ModuleConfigs.SingleOrDefault(c => c.Module == Module.Verification);
        var config = moduleConfig?.Config.Deserialize<VerificationModuleConfig>();

        if (config == null || config.RestrictedChannelIds.Count == 0)
            return PreconditionResult.Success;

        if (config.RestrictedChannelIds.Contains(context.Channel.Id))
            return PreconditionResult.Success;

        await context.Channel.SendMessageAsync(
            MessageFactory.CreateErrorMessage<MessageProperties>("You can't run this command in this channel."));
        return PreconditionResult.Fail(string.Empty);
    }
}