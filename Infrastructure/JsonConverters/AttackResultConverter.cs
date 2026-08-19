using System.Text.Json;
using System.Text.Json.Serialization;
using TornBot.Bot.Domain.Enums;

namespace TornBot.Bot.Infrastructure.JsonConverters;

public class AttackResultConverter : JsonConverter<AttackResult>
{
    public override AttackResult Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String &&
            Enum.TryParse<AttackResult>(reader.GetString(), true, out var result))
        {
            return result;
        }

        throw new JsonException($"Could not convert {reader.GetString()} to {nameof(AttackResult)}");
    }

    public override void Write(Utf8JsonWriter writer, AttackResult value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}