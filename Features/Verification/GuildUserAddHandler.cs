using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NetCord;
using NetCord.Hosting.Gateway;
using NetCord.Rest;
using TornBot.Bot.Domain.Enums;
using TornBot.Bot.Domain.Models;
using TornBot.Bot.Infrastructure;
using TornBot.Bot.Shared;

namespace TornBot.Bot.Features.Verification;

public class GuildUserAddHandler(VerificationService verificationService, TornbotContext context, RestClient client) : IGuildUserAddGatewayHandler
{
    public async ValueTask HandleAsync(GuildUser guildUser)
    {
        var verifiedUser = await verificationService.VerifyGuildUserAsync(guildUser);
        if (verifiedUser == null)
        {
            // TODO: send message
            return;
        }
        
        var faction = await context.Factions
            .Include(f => f.ModuleConfigs)
            .SingleOrDefaultAsync(f => f.GuildId == guildUser.GuildId);

        if (faction == null)
        {
            // TODO: send message
            return;
        }
        
        var config = faction.ModuleConfigs.Single(c => c.Module == Module.Verification).Config.Deserialize<VerificationConfig>();
        if (config == null)
        {
            return;
        }

        await client
            .SendMessageAsync(config.AutoVerificationChannelId,
                MessageFactory.CreateDefaultMessage<MessageProperties>("User verified",
                    $"{guildUser.Nickname} has been verified as {verifiedUser.Nickname}"));
    }
}