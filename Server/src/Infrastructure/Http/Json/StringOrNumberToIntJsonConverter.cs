using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Infrastructure.Http.Json;

/// <summary>
/// 将 JSON 字符串或数字值转换为 <see cref="int"/>。
/// </summary>
internal sealed class StringOrNumberToIntJsonConverter : JsonConverter<int>
{
    /// <summary>
    /// 读取 JSON 并转换为目标响应模型。
    /// </summary>
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Number => reader.GetInt32(),
            JsonTokenType.String when int.TryParse(
                reader.GetString(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int value) => value,
            _ => throw new JsonException($"Cannot convert JSON token {reader.TokenType} to int."),
        };
    }

    /// <summary>
    /// 将目标响应模型写入 JSON。
    /// </summary>
    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}
