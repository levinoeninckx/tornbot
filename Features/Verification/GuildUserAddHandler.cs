using Microsoft.EntityFrameworkCore;
using NetCord;
using NetCord.Hosting.Gateway;
using NetCord.Rest;
using TornBot.Bot.Infrastructure;
using TornBot.Bot.Infrastructure.TornApi;

namespace TornBot.Bot.Features.Verification;

public class GuildUserAddHandler(RestClient restClient, TornApiClient client, TornbotContext context) : IGuildUserAddGatewayHandler
{
    public async ValueTask HandleAsync(GuildUser guildUser)
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
            throw new InvalidOperationException("Faction not configured. Please use the '/configure faction' command first");
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
        
        var message = new MessageProperties()
        {
            Embeds =
            [
                new()
                {
                    Title = "Verified",
                    Description =
                        $"{guildUser.Username} has been verified as [{tornNickname}](https://tcy.sh/p/{tornUserProfile.Id})"
                }
            ],
        };
        
        // TODO: send message
    }
}