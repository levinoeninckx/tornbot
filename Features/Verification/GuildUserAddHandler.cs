using Microsoft.EntityFrameworkCore;
using NetCord;
using NetCord.Hosting.Gateway;
using NetCord.Rest;

namespace TornBot.Bot.Features.Verification;

public class GuildUserAddHandler(VerificationService verificationService, RestClient client) : IGuildUserAddGatewayHandler
{
    public async ValueTask HandleAsync(GuildUser guildUser)
    {
        await verificationService.VerifyGuildUserAsync(guildUser);
    }
}