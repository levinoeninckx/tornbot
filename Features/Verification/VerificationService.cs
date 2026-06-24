using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NetCord;
using NetCord.Rest;
using TornBot.Bot.Domain.Enums;
using TornBot.Bot.Domain.Models;
using TornBot.Bot.Infrastructure;
using TornBot.Bot.Infrastructure.TornApi;

namespace TornBot.Bot.Features.Verification;

public class VerificationService(TornbotContext context, TornApiClient client, RestClient restClient)
{
    public async Task<GuildUser?> VerifyGuildUserAsync(GuildUser guildUser)
    {
        var userId = guildUser.Id;
        var guildId = guildUser.GuildId;

        var profile = await client.GetUserProfileByDiscordId(userId);
        var nickname = $"{profile.Name} [{profile.Id}]";

        var faction = await context.Factions
            .Include(f => f.ModuleConfigs)
            .SingleOrDefaultAsync(f => f.GuildId == guildId);

        if (faction == null)
        {
            // TODO: log
            return null;
        }

        var verificationModule = faction.ModuleConfigs.SingleOrDefault(c => c.Module == Module.Verification);
        var config = verificationModule?.Config.Deserialize<VerificationConfig>();

        if (config == null)
        {
            return null;
        }

        var userFaction = await client.GetUserFactionAsync(profile.Id);
        
        List<ulong> roleIds = [.. config.DefaultRoleIds];
        if (userFaction?.Id == faction.FactionId)
        {
            roleIds.AddRange(config.FactionRoleIds);
        }

        var verifiedUser = await restClient.ModifyGuildUserAsync(guildId, userId, properties => 
        {
            properties.WithNickname(nickname);
            properties.WithRoleIds(roleIds);
        });

        return verifiedUser;
    }
}