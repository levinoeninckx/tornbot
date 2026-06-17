using System.Text;
using FactionBot.Infrastructure.TornApi;
using NetCord;
using NetCord.Services.ApplicationCommands;
using TornBot.Bot.Infrastructure.TornApi;

namespace FactionBot.Features.Wars;

[SlashCommand("war", "all ranked war related commands")]
public class WarCommandModule : ApplicationCommandModule<ApplicationCommandContext>
{
    private TornApiClient _client;

    public WarCommandModule(TornApiClient client)
    {
        _client = client;
    }

    [SubSlashCommand("targets", "shows a list of all hittable targets")]
    public async Task<string> ShowWarTargets()
    {
        // Show all hittable targets of enemy faction
        /*
            Prioritizes:
            High-value targets
            Recently active enemies
        */

        var rankedWars = await _client.GetRankedWarsAsync(41702); //TODO: faction id from appsettings
        var latestWar = rankedWars.RankedWars.First();

        if(latestWar.End != null && latestWar.Winner != null) return $"No active war";

        var opponent = latestWar.Factions.Single(f => f.Id != 41702);

        var response = await _client.GetFactionMembersAsync(opponent.Id);

        var targets = response.Members
            .Where(m => m.Status.State == "Okay")
            .OrderByDescending(m => m.Level)
            .ToList();

        if(targets.Count == 0) return $"No targets found for {opponent.Name}";

        var stringBuilder = new StringBuilder();

        foreach (var member in targets)
        {
            stringBuilder.AppendLine($"[{member.Level}]{member.Name} {member.Status.Description.ToLower()} [attack](https://www.torn.com/loader.php?sid=attack&user2ID={member.Id})");
        }
        
        await SendLongMessageSmartAsync(Context.Channel, stringBuilder.ToString());

        var startDate = DateTimeOffset.FromUnixTimeSeconds(latestWar.Start).DateTime;

        if(startDate > DateTime.UtcNow)
        {
            await Context.Channel.SendMessageAsync($"Note that the war has not yet started");
        }

        return "Available targets";
    }

    [SubSlashCommand("hospital", "shows a list of all enemies in the hospital")]
    public async Task<string> ShowHospitalizedEnemies()
    {
        var rankedWars = await _client.GetRankedWarsAsync(41702); //TODO: faction id from appsettings
        var latestWar = rankedWars.RankedWars.First();

        if(latestWar.End != null && latestWar.Winner != null) return $"No active war";

        var opponent = latestWar.Factions.Single(f => f.Id != 41702);

        var response = await _client.GetFactionMembersAsync(opponent.Id);

        var hospitalizedMembers = response.Members
            .Where(m => m.Status.State == "Hospital")
            .OrderBy(m => m.HasEarlyDischarge)
            .ThenByDescending(m => m.Level)
            .ToList();
        
        if(hospitalizedMembers.Count == 0) return $"No one is in the hospital for {opponent.Name}";

        var stringBuilder = new StringBuilder();

        foreach (var member in hospitalizedMembers)
        {
            var offset = DateTimeOffset.FromUnixTimeSeconds((long)member.Status.Until!);
            stringBuilder.AppendLine($"[{member.Level}]{member.Name} in hospital until {offset.ToString("HH:mm:ss")}");
        }

        await SendLongMessageSmartAsync(Context.Channel, stringBuilder.ToString());

        return $"Hospitalized";
    }

    private static async Task SendLongMessageSmartAsync(TextChannel channel, string content)
    {
        const int maxLength = 2000;

        while (content.Length > 0)
        {
            int length = Math.Min(maxLength, content.Length);

            int splitIndex = content.LastIndexOf('\n', length - 1);

            if (splitIndex <= 0)
                splitIndex = content.LastIndexOf(' ', length - 1);

            if (splitIndex <= 0)
                splitIndex = length;

            string chunk = content.Substring(0, splitIndex).Trim();
            await channel.SendMessageAsync(chunk);

            content = content.Substring(splitIndex).TrimStart();
        }
    }
}
