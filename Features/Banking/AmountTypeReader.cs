using NetCord;
using NetCord.Services.ApplicationCommands;

namespace TornBot.Bot.Features.Banking;

public class AmountTypeReader : SlashCommandTypeReader<ApplicationCommandContext>
{
    public override ApplicationCommandOptionType Type => ApplicationCommandOptionType.String;

    public override ValueTask<SlashCommandTypeReaderResult> ReadAsync(
        string value,
        ApplicationCommandContext context,
        SlashCommandParameter<ApplicationCommandContext> parameter,
        ApplicationCommandServiceConfiguration<ApplicationCommandContext> configuration,
        IServiceProvider? serviceProvider)
    {
        var result = TryParse(value, out var amount);

        if (!result)
        {
            return ValueTask.FromResult(SlashCommandTypeReaderResult.Fail("Invalid amount format. Use a number optionally followed by k, m, or b (e.g. 10k, 2.5m, 1b)."));
        }

        if (amount <= 0)
        {
            return ValueTask.FromResult(SlashCommandTypeReaderResult.Fail("Amount must be greater than 0."));
        }

        return ValueTask.FromResult(SlashCommandTypeReaderResult.Success(amount));
    }

    private static bool TryParse(string value, out long amount)
    {
        amount = 0;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        value = value.Trim().ToLowerInvariant();

        long multiplier = 1;

        if (value.EndsWith('k'))
        {
            multiplier = 1_000;
            value = value[..^1];
        }
        else if (value.EndsWith('m'))
        {
            multiplier = 1_000_000;
            value = value[..^1];
        }
        else if (value.EndsWith('b'))
        {
            multiplier = 1_000_000_000;
            value = value[..^1];
        }

        if (!decimal.TryParse(value, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var number))
            return false;

        number = Math.Round(number, 2);
        var result = number * multiplier;

        if (result != Math.Floor(result))
            return false;

        if (result > long.MaxValue || result < long.MinValue)
            return false;

        amount = (long)result;
        return true;
    }
}
