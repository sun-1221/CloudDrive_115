using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Infrastructure.Http.Json;

/// <summary>
/// Converts JSON string or number values to nullable <see cref="int"/>.
/// </summary>
public sealed class NullableStringOrNumberToIntJsonConverter : JsonConverter<int?>
{
    public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.Number => reader.GetInt32(),
            JsonTokenType.String when string.IsNullOrWhiteSpace(reader.GetString()) => null,
            JsonTokenType.String when int.TryParse(
                reader.GetString(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int value) => value,
            _ => throw new JsonException($"Cannot convert JSON token {reader.TokenType} to nullable int."),
        };
    }

    public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteNumberValue(value.Value);
            return;
        }

        writer.WriteNullValue();
    }
}
