using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;
using TornBot.Bot.Domain.Enums;
using TornBot.Bot.Domain.Models;
using TornBot.Bot.Infrastructure;
using TornBot.Bot.Infrastructure.TornApi;

namespace TornBot.Bot.Features.Members;

public class PollMembersJob(
    TornApiClient client, 
    IDbContextFactory<TornbotContext> dbContextFactory, 
    ILogger<PollMembersJob> logger
    ) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var guildId = Convert.ToUInt64(context.MergedJobDataMap.GetString("guildId"));
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        
        var faction = await dbContext.Factions
            .Include(f => f.MemberStates)
            .SingleOrDefaultAsync(x => x.GuildId == guildId);

        if (faction == null)
        {
            logger.LogWarning($"faction not found for guild {guildId}");
            return;
        }
        
        var members = await client.GetFactionMembersByFactionIdAsync(faction.FactionId);

        var updatedMemberStates = members
            .Select(x => new MemberState
            {
                Id = x.Id,
                Status = Enum.Parse<MemberStatus>(x.Status.State)
            })
            .ToImmutableList();

        var updatedMemberStateDict = updatedMemberStates.ToDictionary(x => x.Id);
        foreach (var memberState in faction.MemberStates)
        {
            var updatedMemberState = updatedMemberStateDict[memberState.Id];
            if (updatedMemberState.Status != memberState.Status)
            {
                //TODO: send event
                switch (updatedMemberState.Status)
                {
                    case MemberStatus.Hospital:
                        // TODO: check if attacked -> retal
                        // TODO: check if overdosed -> od message
                        break;
                }
                memberState.Status = updatedMemberState.Status;
            }
        }
        
        var memberStateLookup = faction.MemberStates.ToLookup(x => x.Id);
        var newMemberStates = updatedMemberStates
            .Where(s => !memberStateLookup.Contains(s.Id))
            .ToList();

        foreach (var memberState in newMemberStates)
        {
            faction.MemberStates.Add(memberState);
        }

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Failed to save member states");
        }
    }
}