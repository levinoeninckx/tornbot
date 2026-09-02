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
    public UserBasicStatus Status { get; set; }
}

public class UserBasicStatus
{
    public required string Description { get; set; }
    public string? Details { get; set; }
    public required string Color { get; set; }
    public required string State { get; set; }
    public int? Until { get; set; }
}