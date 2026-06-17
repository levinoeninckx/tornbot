using discordBotTest.Shared;
using NetCord.Services.ApplicationCommands;

namespace TornBot.Bot.Features.Configurations;

[SlashCommand("configure", "Configure command")]
public class ConfigurationCommandModule(ChannelService channelService)
    : ApplicationCommandModule<ApplicationCommandContext>
{
    [SubSlashCommand("chain", "configure this channel for chain monitoring")]
    public string SetChainMonitoringChannel()
    {
        if(Context.Channel == null) throw new InvalidOperationException();
        var channelId = Context.Channel.Id;

        channelService.AddChannelId(TrackingChannel.Chain, channelId);

        return "This channel is configured for chain monitoring";
    }

    [SubSlashCommand("war", "configure this channel for war monitoring")]
    public string SetWarMonitoringChannel()
    {
        if(Context.Channel == null) throw new InvalidOperationException();
        var channelId = Context.Channel.Id;

        channelService.AddChannelId(TrackingChannel.War, channelId);

        return "This channel is configured for war monitoring";
    }
}
