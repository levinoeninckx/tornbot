using discordBotTest.Shared;
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
using TornBot.Bot.Features.Verification;
using TornBot.Bot.Infrastructure;
using TornBot.Bot.Infrastructure.TornApi;
using TornBot.Bot.Shared;

var builder = Host.CreateApplicationBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

builder.Services.AddLogging();

var connectionString = builder.Configuration["ConnectionStrings:Tornbot"];

if (string.IsNullOrWhiteSpace(connectionString)) throw new InvalidOperationException("Connection string is not set");

builder.Services.AddDbContext<TornbotContext>(options => options.UseNpgsql(connectionString), ServiceLifetime.Transient, ServiceLifetime.Transient);

var discordBotToken = builder.Configuration["Discord:Token"];

if(discordBotToken == null) throw new InvalidOperationException("Discord bot token is not set");

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
    .AddComponentInteractions<ButtonInteraction, ButtonInteractionContext>();

// Httpclient
builder.Services.AddHttpClient<TornApiClient>(client =>
{
    client.BaseAddress = new Uri("https://api.torn.com/v2/");
});

// Set DI services
builder.Services.AddTransient<ApiKeyService>();
builder.Services.AddTransient<VerificationService>();
builder.Services.AddTransient<ModuleConfigRepository>();
builder.Services.AddSingleton<ChannelService>();
builder.Services.AddSingleton<FactionService>();

// Set backgroundservices
builder.Services.AddHostedService<ChainService>();

var host = builder.Build();

host.AddModules(typeof(Program).Assembly);

await host.RunAsync();