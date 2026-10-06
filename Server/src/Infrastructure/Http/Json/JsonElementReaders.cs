using System.Globalization;
using System.Text.Json;

namespace Infrastructure.Http.Json;

/// <summary>
/// 115 响应 JSON 字段读取辅助方法。
/// </summary>
internal static class JsonElementReaders
{
    /// <summary>
    /// 按候选字段名读取字符串值。
    /// </summary>
    internal static string? GetString(JsonElement element, params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            // 115 同一含义字段在不同接口中可能使用缩写或旧名称，按候选顺序取第一个命中值。
            if (element.TryGetProperty(propertyName, out JsonElement property))
            {
                return ReadString(property);
            }
        }

        return null;
    }

    /// <summary>
    /// 按候选字段名读取 32 位整数值。
    /// </summary>
    internal static int? GetInt32(JsonElement element, params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            if (element.TryGetProperty(propertyName, out JsonElement property))
            {
                return ReadInt32(property);
            }
        }

        return null;
    }

    /// <summary>
    /// 按候选字段名读取 64 位整数值。
    /// </summary>
    internal static long? GetInt64(JsonElement element, params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            if (element.TryGetProperty(propertyName, out JsonElement property))
            {
                return ReadInt64(property);
            }
        }

        return null;
    }

    /// <summary>
    /// 按候选字段名读取布尔值。
    /// </summary>
    internal static bool? GetBoolean(JsonElement element, params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            if (element.TryGetProperty(propertyName, out JsonElement property))
            {
                return ReadBoolean(property);
            }
        }

        return null;
    }

    /// <summary>
    /// 读取字符串形式的值。
    /// </summary>
    internal static string? ReadString(JsonElement element)
    {
        // 错误码和文件标识在 115 响应里经常混用数字/布尔/字符串，统一转成文本便于后续匹配。
        return element.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => bool.TrueString,
            JsonValueKind.False => bool.FalseString,
            _ => element.GetRawText(),
        };
    }

    /// <summary>
    /// 读取 JSON 元素中的 32 位整数值。
    /// </summary>
    internal static int? ReadInt32(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.Number => element.GetInt32(),
            JsonValueKind.String when string.IsNullOrWhiteSpace(element.GetString()) => null,
            JsonValueKind.String when int.TryParse(
                element.GetString(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int value) => value,
            _ => null,
        };
    }

    /// <summary>
    /// 读取 JSON 元素中的 64 位整数值。
    /// </summary>
    internal static long? ReadInt64(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.Number => element.GetInt64(),
            JsonValueKind.String when string.IsNullOrWhiteSpace(element.GetString()) => null,
            JsonValueKind.String when long.TryParse(
                element.GetString(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out long value) => value,
            _ => null,
        };
    }

    /// <summary>
    /// 读取 JSON 元素中的布尔值。
    /// </summary>
    internal static bool? ReadBoolean(JsonElement element)
    {
        // state、is_video 等字段既可能是 true/false，也可能是 0/1 或字符串数字。
        return element.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number => element.GetInt64() != 0,
            JsonValueKind.String when string.IsNullOrWhiteSpace(element.GetString()) => null,
            JsonValueKind.String when bool.TryParse(element.GetString(), out bool value) => value,
            JsonValueKind.String when long.TryParse(
                element.GetString(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out long value) => value != 0,
            _ => null,
        };
    }
}
