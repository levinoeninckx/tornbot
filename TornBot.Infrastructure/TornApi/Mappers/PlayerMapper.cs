using TornBot.Bot.Infrastructure.TornApi.Models;
using TornBot.Domain.Enums;
using TornBot.Domain.Models;

namespace TornBot.Infrastructure.TornApi.Mappers;

public static class PlayerMapper
{
    public static Player MapToDomain(Profile profile, Faction? faction = null)
    {
        return new Player
        {
            Id = profile.Id,
            Username = profile.Name,
            Level = profile.Level,
            Status = Enum.Parse<PlayerStatus>(profile.Status.State)
        };
    }
}