namespace TornBot.Bot.Features.Retaliation.Models;

public class BattleStat(long strength, long defense, long speed, long dexterity)
{
    public long Strength { get; private set; } = strength;
    public long Defense { get; private set; } = defense;
    public long Speed { get; private set; } = speed;
    public long Dexterity { get; private set; } = dexterity; 
    public long Total => Strength + Defense + Speed + Dexterity;
}