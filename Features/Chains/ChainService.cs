using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using NetCord.Rest;
using TornBot.Bot.Domain.Enums;
using TornBot.Bot.Infrastructure;
using TornBot.Bot.Infrastructure.TornApi;
using TornBot.Bot.Infrastructure.TornApi.Models;
using TornBot.Bot.Shared;

namespace TornBot.Bot.Features.Chains;

public class ChainService : BackgroundService
{
    private readonly RestClient _restClient;
    private readonly ChannelService _channelService;
    private readonly TornClient _client;
    private readonly IDbContextFactory<TornbotContext> _contextFactory;

    public ChainService(RestClient restClient, ChannelService channelService, TornClient client,
        IDbContextFactory<TornbotContext> contextFactory)
    {
        _restClient = restClient;
        _channelService = channelService;
        _client = client;
        _contextFactory = contextFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var channelid = _channelService.GetChannelId(TrackingChannel.Chain);

            if (!channelid.HasValue) continue;

            await using var context = await _contextFactory.CreateDbContextAsync(stoppingToken);
            var faction = await context.Factions
                .Include(f => f.ApiKeys)
                .FirstOrDefaultAsync(stoppingToken);

            var apiKey = faction?.GetKey(AccessLevel.Public);
            if (apiKey is null) continue;

            var chain = await _client.GetChainStateAsync(apiKey.Key, stoppingToken);
            apiKey.IncreaseUsage();
            await context.SaveChangesAsync(stoppingToken);

            if (chain.Timeout == 0) continue;

            if (chain.Cooldown > 0)
            {
                await _restClient.SendMessageAsync(channelid.Value, $"Chain dropped at {chain.Current} hits",
                    cancellationToken: stoppingToken);
            }

            if (chain.Timeout <= 90)
            {
                var message = CreateDropWarningMessage<MessageProperties>(chain);

                if (message.Content == null)
                    throw new InvalidOperationException("Message content is null");

                await _restClient.SendMessageAsync(channelid.Value, message.Content, cancellationToken: stoppingToken);
            }

            if ((float)chain.Current / chain.Max >= 0.85)
            {
                await _restClient
                    .SendMessageAsync(channelid.Value,
                        $"Approaching {chain.Max} bonus hit, {chain.Max - chain.Current} hits left",
                        cancellationToken: stoppingToken);
            }
        }
    }

    private T CreateDropWarningMessage<T>(ChainState chain) where T : IMessageProperties, new()
    {
        var embedProperties = new EmbedProperties
        {
            Title = "Chain drop warning",
            Description = $"The chain of {chain.Current} hits will break in {chain.Timeout} seconds"
        };

        var message = new T
        {
            Embeds = [embedProperties],
        };

        return message;
    }

    private T CreateMessage<T>(string content) where T : IMessageProperties, new()
    {
        var message = new T
        {
            Content = content,
        };

        return message;
    }
}