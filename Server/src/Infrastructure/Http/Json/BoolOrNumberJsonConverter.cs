using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Infrastructure.Http.Json;

/// <summary>
/// 将 115 响应中的 <c>state</c> 字段从布尔值或数字 0/1 转换为布尔值。
/// </summary>
internal sealed class BoolOrNumberJsonConverter : JsonConverter<bool>
{
    /// <summary>
    /// 读取 JSON 并转换为目标响应模型。
    /// </summary>
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

    /// <summary>
    /// 将目标响应模型写入 JSON。
    /// </summary>
    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
    {
        writer.WriteBooleanValue(value);
    }

    /// <summary>
    /// 读取数字形式的布尔值。
    /// </summary>
    internal static bool ReadNumber(ref Utf8JsonReader reader)
    {
        // 优先按整数读取，只有上游返回浮点数字时才退回 double 判断非零。
        if (reader.TryGetInt64(out long longValue))
        {
            return longValue != 0;
        }

        return reader.GetDouble() != 0d;
    }

    /// <summary>
    /// 读取字符串形式的值。
    /// </summary>
    internal static bool ReadString(string? value)
    {
        // 字符串优先兼容标准布尔文本，再兼容 115 常见的 "0"/"1" 形式。
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
