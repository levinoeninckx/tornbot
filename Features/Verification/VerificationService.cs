using Microsoft.EntityFrameworkCore;
using NetCord;
using NetCord.Rest;
using TornBot.Bot.Infrastructure;
using TornBot.Bot.Infrastructure.TornApi;

namespace TornBot.Bot.Features.Verification;

public class VerificationService(TornbotContext context, TornApiClient client, RestClient restClient)
{
    public async Task<bool> VerifyGuildUserAsync(GuildUser guildUser)
    {
        var tornUserProfile = await client.GetUserProfileByDiscordId(guildUser.Id);
        
        var userFaction = await client.GetUserFactionAsync(tornUserProfile.Id);
        var guildFaction = await context.Factions.SingleOrDefaultAsync(f => f.GuildId == guildUser.GuildId);
        
        var defaultRoles = await context.AuthRoles
            .Include(r => r.Faction)
            .Where(r => r.IsDefault && r.Faction!.GuildId == guildUser.GuildId)
            .ToListAsync();

        if (guildFaction == null)
        {
            // TODO: log
            return false;
        }

        if (userFaction != null)
        {
            if (userFaction.Id == guildFaction.FactionId)
            {
                var factionRoles = await context.AuthRoles
                    .Include(r => r.Faction)
                    .Where(r => r.IsFaction && r.Faction!.GuildId == guildUser.GuildId)
                    .ToListAsync();
            
                defaultRoles.AddRange(factionRoles);
            }
        }
        
        var tornNickname = $"{tornUserProfile.Name} [{tornUserProfile.Id}]";
        await restClient.ModifyGuildUserAsync(guildUser.GuildId, guildUser.Id,
            properties =>
            {
                properties
                    .WithNickname(tornNickname)
                    .AddRoleIds(defaultRoles.Select(r => r.RoleId).ToList());
            });
        
        return true;
    }
}