using NetCord.Services.ApplicationCommands;
using Quartz;
using TornBot.Bot.Domain.Enums;
using TornBot.Bot.Infrastructure;
using TornBot.Bot.Shared;

namespace TornBot.Bot.Features.OrganizedCrime;

[RequireKey(AccessLevel.Minimal, true)]
[SlashCommand("oc", "organized crime related commands")]
public class OrganizedCrimeCommandModule(ISchedulerFactory schedulerFactory, ModuleConfigRepository repository) : ApplicationCommandModule<ApplicationCommandContext>
{
    
    [SubSlashCommand("getcrimes", "get new crimes in background")]
    public async Task GetNewCrimes()
    {
    }
}