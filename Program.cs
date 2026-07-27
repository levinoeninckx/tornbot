using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetCord;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services;
using NetCord.Hosting.Services.ApplicationCommands;
using NetCord.Hosting.Services.ComponentInteractions;
using NetCord.Services.ComponentInteractions;
using Quartz;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using TornBot.Bot.Features.Chains;
using TornBot.Bot.Features.Retaliation;
using TornBot.Bot.Features.Verification;
using TornBot.Bot.Infrastructure;
using TornBot.Bot.Infrastructure.BackgroundJobs;
using TornBot.Bot.Infrastructure.FFScouter;
using TornBot.Bot.Infrastructure.TornApi;
using TornBot.Bot.Infrastructure.TornStats;
using TornBot.Bot.Shared;

var builder = Host.CreateApplicationBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

builder.Services.AddSerilog(configure =>
{
    configure.WriteTo.Console();

    configure.MinimumLevel.Override("Microsoft.AspNetCore.Hosting", LogEventLevel.Warning);
    configure.MinimumLevel.Override("Microsoft.AspNetCore.Mvc", LogEventLevel.Warning);
    configure.MinimumLevel.Override("Microsoft.AspNetCore.Routing", LogEventLevel.Warning);

    if (builder.Environment.IsDevelopment())
        return;

    var seqApiKey = builder.Configuration["seq:apiKey"];
    var seqUrl = builder.Configuration["seq:url"];
    if (seqApiKey == null || seqUrl == null) return;

    var levelSwitch = new LoggingLevelSwitch();
    configure.MinimumLevel.ControlledBy(levelSwitch);
    configure.WriteTo.Seq(seqUrl, apiKey: seqApiKey, controlLevelSwitch: levelSwitch);
});

var connectionString = builder.Configuration["ConnectionStrings:Tornbot"];

if (string.IsNullOrWhiteSpace(connectionString)) throw new InvalidOperationException("Connection string is not set");

builder.Services.AddDbContextFactory<TornbotContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddQuartz(q =>
{
    q.UseInMemoryStore();
    q.UseSimpleTypeLoader();
    q.UseDefaultThreadPool(p => p.MaxConcurrency = 10);

    q.ScheduleJob<UpdateOrganizedCrimes>(t =>
        t.StartNow().WithSimpleSchedule(x => x.WithIntervalInSeconds(30).RepeatForever()));
    q.ScheduleJob<UpdateRetalsJob>(trigger =>
        trigger.StartNow().WithSimpleSchedule(x => x.WithIntervalInSeconds(30).RepeatForever()));
});

builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

// Netcord
var discordBotToken = builder.Configuration["Discord:Token"];
if (discordBotToken == null) throw new InvalidOperationException("Discord bot token is not set");

builder.Services
    .AddDiscordGateway(options =>
    {
        options.Token = discordBotToken;
        options.Intents = GatewayIntents.GuildUsers | GatewayIntents.AllNonPrivileged;
    })
    .AddGatewayHandlers(typeof(Program).Assembly)
    .AddApplicationCommands();

// Component interactions
builder.Services
    .AddComponentInteractions<RoleMenuInteraction, RoleMenuInteractionContext>()
    .AddComponentInteractions<ChannelMenuInteraction, ChannelMenuInteractionContext>()
    .AddComponentInteractions<ButtonInteraction, ButtonInteractionContext>()
    .AddComponentInteractions<StringMenuInteraction, StringMenuInteractionContext>();

// Httpclient
builder.Services.AddHttpClient<TornApiClient>(client => client.BaseAddress = new Uri("https://api.torn.com/v2/"));
builder.Services.AddHttpClient<AttackService>(client =>
    client.BaseAddress = new Uri("https://api.torn.com/v2/faction/attacksfull/"));
builder.Services.AddHttpClient<FfScouterClient>(client =>
    client.BaseAddress = new Uri("https://ffscouter.com/api/v1/"));
builder.Services.AddHttpClient<TornStatClient>(client =>
    client.BaseAddress = new Uri("https://www.tornstats.com/api/v2/"));

// Set DI services
builder.Services.AddTransient<ApiKeyService>();
builder.Services.AddTransient<VerificationService>();
builder.Services.AddTransient<ModuleConfigRepository>();
builder.Services.AddTransient<NotificationService>();
builder.Services.AddSingleton<ChannelService>();
builder.Services.AddSingleton<FactionService>();
builder.Services.AddTransient<BattleStatService>();

// Set backgroundservices
builder.Services.AddHostedService<ChainService>();

var host = builder.Build();

host.AddModules(typeof(Program).Assembly);

await host.RunAsync();