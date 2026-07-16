using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Rest;
using Quartz;
using TornBot.Bot.Infrastructure;
using TornBot.Bot.Infrastructure.TornApi;
using TornBot.Bot.Shared;

namespace TornBot.Bot.Features.Retaliation;

public class GetOutgoingAttacks(AttackService attackService, IDbContextFactory<TornbotContext> contextFactory, TornApiClient tornClient, ModuleConfigRepository repository, RestClient client, ILogger<GetOutgoingAttacks> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        
        var factions = await dbContext.Factions
            .Include(f => f.TrackedAttacks)
            .ToListAsync();

        foreach (var faction in factions)
        {
            var attacks = await attackService.GetOutgoingAttacks(faction.GuildId);
            var opportunityDict = faction.TrackedAttacks.ToImmutableDictionary(a => a.TargetPlayerId);
            var config = await repository.GetRetalModuleConfigByGuildId(faction.GuildId);
            foreach (var attack in attacks)
            {
                if (!opportunityDict.Keys.Contains(attack.Defender.Id)) continue;
                var opportunity = opportunityDict[attack.Defender.Id];

                if (attack.Attacker is null)
                {
                    logger.LogError("Something went wrong in getting data for outgoing attacks for faction {FactionId}", faction.Id);
                    continue;
                }
                    
                var attackerBasic = await tornClient.GetUserProfileById(attack.Attacker.Id);
                if (attackerBasic == null) continue;
                
                var message = await client.GetMessageAsync(config!.NotificationChannelId!.Value, opportunity.MessageId);
                await client.ModifyMessageAsync(config!.NotificationChannelId!.Value, opportunity.MessageId,
                    messageProperties =>
                    {
                        messageProperties.Embeds =
                        [
                            new EmbedProperties
                            {
                                Title = $"Retal claimed by [{attackerBasic.Name}]({ShortUrlHelper.GetProfileUrl(attackerBasic.Id)})",
                                Description = message.Embeds[0].Description,
                                Color = new Color(0, 255, 255)
                            }
                        ];
                        messageProperties.Components = [];
                    });
                
                faction.TrackedAttacks.Remove(opportunity);
            }
        }

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (Exception e)
        {
            logger.LogCritical(e, "Failed to remove retal opportunities");
        }
    }
}