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

public class GuildUserAddHandler(VerificationService verificationService, TornbotContext context, RestClient client, ILogger<GuildUserAddHandler> logger) : IGuildUserAddGatewayHandler
{
    public async ValueTask HandleAsync(GuildUser guildUser)
    {
        var faction = await context.Factions
            .Include(f => f.ModuleConfigs)
            .SingleOrDefaultAsync(f => f.GuildId == guildUser.GuildId);

        if (faction == null)
        {
            logger.LogWarning($"faction not found for guild {guildUser.GuildId}, needs to be registered with /configure bot");
            return;
        }
        
        var config = faction.ModuleConfigs.Single(c => c.Module == Module.Verification).Config.Deserialize<VerificationConfig>();
        if (config == null)
        {
            logger.LogWarning($"faction not found for guild {guildUser.GuildId}, needs to be registered with /configure bot");
            return;
        }

        var verifiedUser = await verificationService.VerifyGuildUserAsync(guildUser);
        if (verifiedUser == null)
        {
            await client
                .SendMessageAsync(config.AutoVerificationChannelId,
                    MessageFactory.CreateErrorMessage<MessageProperties>("Failed to verify automatically, please use /verify"));
            return;
        }
        
        await client
            .SendMessageAsync(config.AutoVerificationChannelId,
                MessageFactory.CreateDefaultMessage<MessageProperties>("User verified",
                    $"{guildUser.Nickname} has been verified as {verifiedUser.Nickname}"));
    }
}