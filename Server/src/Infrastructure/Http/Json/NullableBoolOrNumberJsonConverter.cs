using System.Text.Json;
using System.Text.Json.Serialization;

namespace Infrastructure.Http.Json;

/// <summary>
/// 支持空值的 <see cref="BoolOrNumberJsonConverter"/> 变体。
/// </summary>
internal sealed class NullableBoolOrNumberJsonConverter : JsonConverter<bool?>
{
    /// <summary>
    /// 读取 JSON 并转换为目标响应模型。
    /// </summary>
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

    /// <summary>
    /// 将目标响应模型写入 JSON。
    /// </summary>
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
