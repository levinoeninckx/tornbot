using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Rest;
using Quartz;
using TornBot.Bot.Infrastructure;
using TornBot.Bot.Infrastructure.TornApi;

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
                if(opportunityDict.Keys.Contains(attack.Defender.Id))
                {
                    //TODO: retal claimed
                    //TODO: modify message to say retal claimed by <player> -> amount of respect gained, maybe link to attack log
                    //TODO: delete tracked attack
                    var opportunity = opportunityDict[attack.Defender.Id];

                    var attackerBasic = await tornClient.GetUserProfileById(attack.Attacker.Id);
                    if (attackerBasic == null) continue;
                    await client.ModifyMessageAsync(config!.ChannelId!.Value, opportunity.MessageId,
                        messageProperties =>
                        {
                            messageProperties.Embeds =
                            [
                                new EmbedProperties
                                {
                                    Title = "Retal claimed",
                                    Description =
                                        $"Retal claimed by [{attackerBasic.Name}](https://torn.com/profile.php?XID={attackerBasic.Id})",
                                    Color = new Color(0x00FF00),
                                }
                            ];
                            messageProperties.Components = [];
                        });
                }
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