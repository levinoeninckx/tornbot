using System;
using System.Drawing;

namespace FactionBot.Features.Wars;

public class FactionMemberStatus
{
    public required string Description { get; set; }
    public string? Details { get; set; }
    public required string State { get; set; }
    public int? Until { get; set; }
}
