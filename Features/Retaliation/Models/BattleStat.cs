using System.Text;

namespace TornBot.Bot.Features.Retaliation.Models;

public class BattleStat(ulong strength, ulong defense, ulong speed, ulong dexterity)
{
    public ulong Strength { get; private set; } = strength;
    public ulong Defense { get; private set; } = defense;
    public ulong Speed { get; private set; } = speed;
    public ulong Dexterity { get; private set; } = dexterity; 
    public ulong Total => Strength + Defense + Speed + Dexterity;
    public string TotalHumanReadable => FormatValue(Total);
    public string StrengthHumanReadable => FormatValue(Strength);
    public string DefenseHumanReadable => FormatValue(Defense);
    public string SpeedHumanReadable => FormatValue(Speed);
    public string DexterityHumanReadable => FormatValue(Dexterity);

    private static string FormatValue(ulong value)
    {
        var doubleValue = (double)value;

        if (doubleValue >= 1_000_000_000)
            return $"{doubleValue / 1_000_000_000:0.##}b";
        if (doubleValue >= 1_000_000)
            return $"{doubleValue / 1_000_000:0.##}m";
        if (doubleValue >= 1_000)
            return $"{doubleValue / 1_000:0.##}k";

        return doubleValue.ToString("0");
    }

    public override string ToString()
    {
        var stringBuilder = new StringBuilder();
        
        stringBuilder.AppendLine($"Total: {TotalHumanReadable}");
        stringBuilder.AppendLine($"Strength: {StrengthHumanReadable}");
        stringBuilder.AppendLine($"Defense: {DefenseHumanReadable}");
        stringBuilder.AppendLine($"Speed: {SpeedHumanReadable}");
        stringBuilder.AppendLine($"Dexterity: {DexterityHumanReadable}");
        
        return stringBuilder.ToString();
    }
}