using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Infrastructure.Http.Json;

/// <summary>
/// Converts the 115 <c>state</c> field from either JSON boolean or numeric 0/1 values.
/// </summary>
public sealed class BoolOrNumberJsonConverter : JsonConverter<bool>
{
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.Number => ReadNumber(ref reader),
            JsonTokenType.String => ReadString(reader.GetString()),
            _ => throw new JsonException($"Cannot convert JSON token {reader.TokenType} to bool."),
        };
    }

    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
    {
        writer.WriteBooleanValue(value);
    }

    internal static bool ReadNumber(ref Utf8JsonReader reader)
    {
        if (reader.TryGetInt64(out long longValue))
        {
            return longValue != 0;
        }

        return reader.GetDouble() != 0d;
    }

    internal static bool ReadString(string? value)
    {
        if (bool.TryParse(value, out bool boolValue))
        {
            return boolValue;
        }

        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long longValue))
        {
            return longValue != 0;
        }

        throw new JsonException($"Cannot convert JSON string '{value}' to bool.");
    }
}
