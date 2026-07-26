using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Rest;
using TornBot.Bot.Domain.Models;
using TornBot.Bot.Infrastructure;
using TornBot.Bot.Infrastructure.BackgroundJobs;
using TornBot.Bot.Infrastructure.TornApi;

namespace TornBot.Bot.Features.Retaliation;

public class GetOutgoingAttacks(
    AttackService attackService,
    IDbContextFactory<TornbotContext> contextFactory,
    TornApiClient tornClient,
    ModuleConfigRepository repository,
    RestClient client,
    ILogger<GetOutgoingAttacks> logger
) : FactionJob<GetOutgoingAttacks>(contextFactory, logger)
{
    protected override Task<List<Faction>> LoadFactionsAsync(TornbotContext dbContext, CancellationToken ct)
    {
        return dbContext.Factions
            .Include(f => f.TrackedAttacks)
            .ToListAsync(ct);
    }

    protected override async Task ProcessFactionAsync(Faction faction, CancellationToken ct)
    {
        var attacks = await attackService.GetOutgoingAttacks(faction.GuildId);
        var opportunityDict = faction.TrackedAttacks.ToImmutableDictionary(a => a.TargetPlayerId);
        var config = await repository.GetRetalModuleConfigByGuildId(faction.GuildId);

        foreach (var attack in attacks)
        {
            if (!opportunityDict.TryGetValue(attack.Defender.Id, out var opportunity)) continue;

            if (attack.Attacker is null)
            {
                Logger.LogError("Something went wrong in getting data for outgoing attacks for faction {FactionId}",
                    faction.Id);
                continue;
            }

            var attackerBasic = await tornClient.GetUserProfileById(attack.Attacker.Id, ct);
            if (attackerBasic == null) continue;

            var message = await client.GetMessageAsync(config!.NotificationChannelId!.Value, opportunity.MessageId,
                cancellationToken: ct);
            await client.ModifyMessageAsync(config.NotificationChannelId!.Value, opportunity.MessageId,
                messageProperties =>
                {
                    messageProperties.Embeds =
                    [
                        new EmbedProperties
                        {
                            Title = $"Retal claimed by {attackerBasic.Name}[{attackerBasic.Id}]",
                            Description = message.Embeds[0].Description,
                            Color = new Color(0, 255, 0)
                        }
                    ];
                    messageProperties.Components = [];
                }, cancellationToken: ct);

            faction.TrackedAttacks.Remove(opportunity);
        }
    }
}