using System;
using FactionBot.Features.Wars;

namespace TornBot.Bot.Infrastructure.TornApi.Models;

public class UserBasicResponse
{
    public Profile Profile { get; set; }
}

public class Profile
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Level { get; set; }
    public string Gender { get; set; }
    public FactionMemberStatus Status { get; set; }
}
