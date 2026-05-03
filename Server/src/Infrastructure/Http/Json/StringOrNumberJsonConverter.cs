using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;

namespace Infrastructure.Http.Json;

/// <summary>
/// Keeps inconsistent 115 error-code fields as strings regardless of whether JSON uses a number or a string.
/// </summary>
public sealed class StringOrNumberJsonConverter : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => ReadNumberAsString(ref reader),
            JsonTokenType.True => bool.TrueString,
            JsonTokenType.False => bool.FalseString,
            _ => JsonDocument.ParseValue(ref reader).RootElement.GetRawText(),
        };
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value);
    }

    private static string ReadNumberAsString(ref Utf8JsonReader reader)
    {
        if (reader.TryGetInt64(out long longValue))
        {
            return longValue.ToString(CultureInfo.InvariantCulture);
        }

        if (reader.TryGetDecimal(out decimal decimalValue))
        {
            return decimalValue.ToString(CultureInfo.InvariantCulture);
        }

        return reader.GetDouble().ToString(CultureInfo.InvariantCulture);
    }
}
