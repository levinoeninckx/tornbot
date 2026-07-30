using System.Text;

namespace TornBot.Bot.Features.Retaliation.Models;

public class BattleStat
{
    public BattleStatDetails? Details { get; set; }
    public ulong Estimate { get; private set; }

    public BattleStat(ulong estimate)
    {
        Estimate = estimate;
    }
    
    public BattleStat(ulong strength, ulong defense, ulong speed, ulong dexterity)
    {
        Estimate = strength + defense + speed + dexterity;
        Details = new BattleStatDetails(strength, defense, speed, dexterity);
    }
    
    public string TotalHumanReadable => Details?.TotalHumanReadable ?? FormatValue(Estimate);

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

    public class BattleStatDetails(ulong strength, ulong defense, ulong speed, ulong dexterity)
    {
        public ulong Total => strength + defense + speed + dexterity;
        public string TotalHumanReadable => FormatValue(Total);
        public string StrengthHumanReadable => FormatValue(strength);
        public string DefenseHumanReadable => FormatValue(defense);
        public string SpeedHumanReadable => FormatValue(speed);
        public string DexterityHumanReadable => FormatValue(dexterity);
    }
}