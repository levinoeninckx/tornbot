using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Hosting.Gateway;
using NetCord.Rest;
using TornBot.Bot.Domain.Enums;
using TornBot.Bot.Domain.Models;
using TornBot.Bot.Infrastructure;
using TornBot.Bot.Shared;

namespace TornBot.Bot.Features.Verification;

public class GuildUserAddHandler(
    VerificationService verificationService,
    IDbContextFactory<TornbotContext> contextFactory,
    RestClient client,
    ILogger<GuildUserAddHandler> logger) : IGuildUserAddGatewayHandler
{
    public async ValueTask HandleAsync(GuildUser guildUser)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var faction = await context.Factions
            .Include(f => f.ModuleConfigs)
            .SingleOrDefaultAsync(f => f.GuildId == guildUser.GuildId);

        if (faction == null)
        {
            logger.LogWarning(
                $"faction not found for guild {guildUser.GuildId}, needs to be registered with /configure bot");
            return;
        }

        var config = faction.ModuleConfigs.Single(c => c.Module == Module.Verification).Config
            .Deserialize<VerificationModuleConfig>();
        if (config == null)
        {
            logger.LogWarning(
                $"faction not found for guild {guildUser.GuildId}, needs to be registered with /configure bot");
            return;
        }

        var userProfile = await verificationService.VerifyGuildUserAsync(guildUser);
        if (userProfile == null)
        {
            await client
                .SendMessageAsync(config.AutoVerificationChannelId,
                    MessageFactory
                        .CreateDefaultMessage<MessageProperties>("Verification failed",
                            "Failed to verify automatically, make sure you have joined the official Torn discord then use the /verify command"));
            return;
        }

        await client
            .SendMessageAsync(config.AutoVerificationChannelId,
                MessageFactory.CreateDefaultMessage<MessageProperties>("User verified",
                    $"{guildUser.Nickname} has been verified as [{userProfile.Name}]({ShortUrlHelper.GetProfileUrl(userProfile.Id)})"));
    }
}