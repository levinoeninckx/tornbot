using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Rest;
using TornBot.Bot.Domain.Enums;
using TornBot.Bot.Domain.Models;
using TornBot.Bot.Infrastructure;
using TornBot.Bot.Infrastructure.TornApi;
using TornBot.Bot.Infrastructure.TornApi.Models;

namespace TornBot.Bot.Features.Verification;

public class VerificationService(
    IDbContextFactory<TornbotContext> contextFactory,
    TornApiClient client,
    RestClient restClient,
    ILogger<VerificationService> logger)
{
    public async Task<Profile?> VerifyGuildUserAsync(GuildUser guildUser)
    {
        var userId = guildUser.Id;
        var guildId = guildUser.GuildId;

        await using var context = await contextFactory.CreateDbContextAsync();
        var faction = await context.Factions
            .Include(f => f.ModuleConfigs)
            .Include(f => f.ApiKeys)
            .SingleOrDefaultAsync(f => f.GuildId == guildId);

        if (faction == null)
        {
            logger.LogWarning($"faction not found for guild {guildId}");
            return null;
        }

        var publicKey = faction.GetApiKey(AccessLevel.Public);
        if (publicKey == null)
        {
            logger.LogWarning($"no public api key found for faction {faction.FactionId}");
            return null;
        }

        var profile = await client.GetUserProfileByDiscordId(userId, publicKey.Key);
        var nickname = $"{profile.Name} [{profile.Id}]";

        var verificationModule = faction.ModuleConfigs.SingleOrDefault(c => c.Module == Module.Verification);
        var config = verificationModule?.Config.Deserialize<VerificationConfig>();

        if (config == null)
        {
            logger.LogError($"verification config not found for faction {faction.FactionId}");
            return null;
        }

        var userFaction = await client.GetUserFactionAsync(profile.Id, publicKey.Key);

        List<ulong> roleIds = [.. config.DefaultRoleIds];
        if (userFaction?.Id == faction.FactionId)
        {
            roleIds.AddRange(config.FactionRoleIds);
        }
        else
        {
            roleIds.AddRange(config.NonFactionRoleIds);
        }

        await restClient.ModifyGuildUserAsync(guildId, userId, properties =>
        {
            properties.WithNickname(nickname);
            properties.WithRoleIds(roleIds.Distinct());
        });

        return profile;
    }
}