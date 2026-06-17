using System;

namespace discordBotTest.Shared;

public class ChannelService
{
    private readonly Dictionary<TrackingChannel, ulong> _channelDictionary = [];

    public ulong? GetChannelId(TrackingChannel trackingChannel)
    {
        if(!_channelDictionary.TryGetValue(trackingChannel, out var channelId))
        {
            return null;
        }

        return channelId;
    }

    public void AddChannelId(TrackingChannel trackingChannel, ulong channelId) => _channelDictionary.Add(trackingChannel, channelId);
}
