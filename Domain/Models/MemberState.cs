using TornBot.Bot.Domain.Enums;

namespace TornBot.Bot.Domain.Models;

public class MemberState
{
    public int Id { get; set; }
    public MemberStatus Status { get; set; }
}