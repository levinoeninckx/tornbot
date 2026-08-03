namespace TornBot.Bot.Shared;

public static class NumberExtensions
{
    public static string ToHumanReadable(this ulong number)
    {
        return number switch
        {
            >= 1_000_000_000_000 => (number / 1_000_000_000_000D).ToString("0.#") + "t",
            >= 1_000_000_000 => (number / 1_000_000_000D).ToString("0.#") + "b",
            >= 1_000_000 => (number / 1_000_000D).ToString("0.#") + "m",
            >= 1_000 => (number / 1_000D).ToString("0.#") + "k",
            _ => number.ToString()
        };
    }
    
    public static string ToHumanReadable(this long number)
    {
        if (number == 0) return "0";

        var sign = number < 0 ? "-" : "";
        var absoluteNumber = Math.Abs(number);

        return sign + ToHumanReadable((ulong)absoluteNumber);
    }
    
    public static string ToHumanReadable(this int number) => ((long)number).ToHumanReadable();
}