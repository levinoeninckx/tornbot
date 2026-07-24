using TornBot.Domain.Enums;

namespace TornBot.Domain.Models;

public class Player
{
    public int Id { get; set; }
    public PlayerStatus Status { get; set; } = PlayerStatus.Okay;
    public int Level { get; set; }
    public string Username { get; set; } = "";
}