using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Infrastructure.Http.Json;

/// <summary>
/// 将 115 不一致的错误码字段统一保留为字符串，兼容 JSON 中的数字和字符串。
/// </summary>
internal sealed class StringOrNumberJsonConverter : JsonConverter<string?>
{
    /// <summary>
    /// 读取 JSON 并转换为目标响应模型。
    /// </summary>
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

    /// <summary>
    /// 将目标响应模型写入 JSON。
    /// </summary>
    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value);
    }

    /// <summary>
    /// 将 JSON 数字读取为字符串。
    /// </summary>
    private static string ReadNumberAsString(ref Utf8JsonReader reader)
    {
        // 先保留整数精度，再退回 decimal/double，避免大文件 ID 或错误码被科学计数法破坏。
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
