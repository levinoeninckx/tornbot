using System;
using System.Text;
using discordBotTest.Shared;
using FactionBot.Infrastructure.TornApi;
using FactionBot.Infrastructure.TornApi.Models;
using Microsoft.Extensions.Hosting;
using NetCord;
using NetCord.Rest;
using TornBot.Bot.Infrastructure.TornApi;
using Channel = NetCord.Channel;

namespace TornBot.Bot.Features.Wars;

public class WarService : BackgroundService
{
    private ChannelService _channelService;
    private RestClient _restClient;
    private TornApiClient _client;
    private Dictionary<int, FactionMember> _trackedMembersDictionary = [];
    private string _tornCityShortAttackBaseUrl = "https://tcy.sh/a/";

    public WarService(ChannelService channelService, RestClient restClient, TornApiClient client)
    {
        _channelService = channelService;
        _restClient = restClient;
        _client = client;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var channelId = _channelService.GetChannelId(TrackingChannel.War);
            if(channelId == null) continue;

            var rankedWars = await _client.GetRankedWarsAsync(41702); // TODO: move faction id to somewhere else
            var latestWar = rankedWars.RankedWars.First();

            if(latestWar.End != 0)
            {
                _trackedMembersDictionary.Clear();
                continue;
            }

            var enemyFactionId = latestWar.Factions.Single(f => f.Id != 41702).Id;

            var membersResponse = await _client.GetFactionMembersAsync(enemyFactionId);
            var enemyMembers = membersResponse.Members;

            if(enemyMembers.Count <= 0) continue;


            if(_trackedMembersDictionary.Count == 0)
            {
                _trackedMembersDictionary = enemyMembers.ToDictionary(m => m.Id);
                // Send large status message                
                var stringBuilder = new StringBuilder();
                foreach (var member in _trackedMembersDictionary.Values)
                {
                    if(member.Status.State == "Okay")
                    {
                        stringBuilder.AppendLine($"[{member.Level}] {member.Name} {member.Status.State} [attack]({_tornCityShortAttackBaseUrl}{member.Id})");
                        continue;
                    }
                    stringBuilder.AppendLine($"[{member.Level}]{member.Name} {member.Status.Description.ToLower()}");
                }
                
                await SendLongMessageSmartAsync(channelId.Value, stringBuilder.ToString());
                continue;
            }

            // check individual member states
            foreach (var member in enemyMembers)
            {
                var currentState = _trackedMembersDictionary[member.Id].Status.State;

                if(member.Status.State != currentState)
                {
                    _trackedMembersDictionary[member.Id] = member;
                    if(member.Status.State == "Okay")
                    {
                        await _restClient.SendMessageAsync(channelId.Value, $"[{member.Level}] {member.Name} {member.Status.State} [attack]({_tornCityShortAttackBaseUrl}{member.Id})");
                        continue;
                    }
                    await _restClient.SendMessageAsync(channelId.Value, $"[{member.Level}] {member.Name} {currentState} -> {member.Status.Description}");
                }

                // Soon to be released from hosiptal time <= 5 min
                if(member.Status.State == "Hospital")
                {
                    var offset = DateTimeOffset.FromUnixTimeSeconds(member.Status.Until!.Value);
                    var minutesLeft = (offset.DateTime - DateTime.Now).Minutes;

                    if(minutesLeft > 5) continue;
                    if(minutesLeft <= 5 && minutesLeft > 0)
                    {
                        await _restClient.SendMessageAsync(channelId.Value, $"[{member.Level}] {member.Name} releases in {minutesLeft} minutes");
                        continue;
                    }

                    var secondsLeft = (offset.DateTime - DateTime.Now).Seconds;
                    await _restClient.SendMessageAsync(channelId.Value, $"[{member.Level}] {member.Name} releases in {secondsLeft} seconds");
                }
            }

        }
    }

    private async Task SendLongMessageSmartAsync(ulong channelId, string content)
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
            await  _restClient.SendMessageAsync(channelId, chunk);

            content = content.Substring(splitIndex).TrimStart();
        }
    }
}
