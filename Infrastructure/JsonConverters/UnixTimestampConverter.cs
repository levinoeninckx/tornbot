using System.Text.Json;
using System.Text.Json.Serialization;

namespace TornBot.Bot.Infrastructure.JsonConverters;

public class UnixTimestampConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            var timestamp = reader.GetInt64();
            if (timestamp == 0)
            {
                return null;
            }

            return DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;
        }

        if (reader.TokenType == JsonTokenType.String && long.TryParse(reader.GetString(), out var seconds))
        {
            if (seconds == 0)
            {
                return null;
            }

            return DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;
        }

        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        return default;
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value == null || value == default)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteNumberValue(new DateTimeOffset(value.Value).ToUnixTimeSeconds());
        }
    }
}