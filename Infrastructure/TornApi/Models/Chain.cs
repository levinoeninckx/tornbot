using System;

namespace discordBotTest.Features.Chains;

public class ChainState
{
    public int Id { get; set; }
    public int Current { get; set; }
    public int Max { get; set; }
    public int Timeout { get; set; }
    public float Modifier { get; set; }
    public int Cooldown { get; set; }
    public int Start { get; set; }
    public int End { get; set; }
}

public class CompletedChain
{
    public int Id { get; set; }
    public int Chain { get; set; }
    public float Respect { get; set; }
    public int Start { get; set; }
    public int End { get; set; }
}

public class FactionChainsResponse
{
    public CompletedChain[] Chains { get; set; } = [];
}