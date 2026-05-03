using System.Text.Json;
using System.Text.Json.Serialization;

namespace Infrastructure.Http.Json;

/// <summary>
/// Nullable variant of <see cref="BoolOrNumberJsonConverter"/>.
/// </summary>
public sealed class NullableBoolOrNumberJsonConverter : JsonConverter<bool?>
{
    public override bool? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.Number => BoolOrNumberJsonConverter.ReadNumber(ref reader),
            JsonTokenType.String => string.IsNullOrWhiteSpace(reader.GetString())
                ? null
                : BoolOrNumberJsonConverter.ReadString(reader.GetString()),
            _ => throw new JsonException($"Cannot convert JSON token {reader.TokenType} to nullable bool."),
        };
    }

    public override void Write(Utf8JsonWriter writer, bool? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteBooleanValue(value.Value);
            return;
        }

        writer.WriteNullValue();
    }
}
