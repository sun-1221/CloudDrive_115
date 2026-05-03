using System.Collections.ObjectModel;
using System.Text.Json;

using Application.Abstractions.CloudDrive;

using Infrastructure.Http.Json;

namespace Infrastructure.Http;

/// <summary>
/// 115 网页端错误字段适配器。按 hostname + path 匹配不同接口的错误字段变体。
/// </summary>
internal sealed class ErrorFieldAdapter : IErrorFieldAdapter
{
    /// <summary>
    /// 获取按接口路径注册的 115 错误字段描述符。
    /// </summary>
    private static readonly IReadOnlyDictionary<string, ErrorFieldDescriptor> Descriptors =
        new ReadOnlyDictionary<string, ErrorFieldDescriptor>(CreateDescriptors());

    /// <summary>
    /// 尝试从 115 API 响应中解析业务错误。
    /// </summary>
    /// <param name="requestUri">当前请求 URI。</param>
    /// <param name="responseJson">115 API 响应 JSON。</param>
    /// <returns>解析到业务错误时返回错误信息；业务成功时返回 <see langword="null"/>。</returns>
    public CloudDriveApiError? TryParse(Uri requestUri, JsonDocument responseJson)
    {
        ArgumentNullException.ThrowIfNull(requestUri);
        ArgumentNullException.ThrowIfNull(responseJson);

        ErrorFieldDescriptor descriptor = ResolveDescriptor(requestUri);
        CloudDriveApiError? error = TryParseWithDescriptor(requestUri, responseJson, descriptor);
        if (error is not null)
        {
            return error;
        }

        // state=false 但没有默认错误字段时不能吞掉，否则调用方会把未知失败当成成功响应。
        if (IsStateFailure(responseJson) && !HasAnyDefaultErrorField(responseJson))
        {
            return new CloudDriveApiError(null, null, "unknown_error_shape", requestUri);
        }

        return null;
    }

    /// <summary>
    /// 获取已注册的错误字段描述符。
    /// </summary>
    internal static IReadOnlyDictionary<string, ErrorFieldDescriptor> RegisteredDescriptors => Descriptors;

    /// <summary>
    /// 创建 115 API 错误字段描述符注册表。
    /// </summary>
    private static Dictionary<string, ErrorFieldDescriptor> CreateDescriptors()
    {
        var descriptors = new Dictionary<string, ErrorFieldDescriptor>(StringComparer.OrdinalIgnoreCase)
        {
            ["webapi.115.com/files/search"] = new("/files/search", "errCode", ErrorCodeType.Number, "error", null, "errMsg"),
            ["webapi.115.com/movies/subtitle"] = new("/movies/subtitle", "errNo", ErrorCodeType.Number, "error"),
            ["webapi.115.com/user/vip_limit"] = new("/user/vip_limit", "code", ErrorCodeType.String, "message", null, "msg"),
            ["webapi.115.com/user/index_info"] = new("/user/index_info", "code", ErrorCodeType.String, "message", null, "msg"),
            ["webapi.115.com/files/index_info"] = new("/files/index_info", "code", ErrorCodeType.String, "message", null, "msg"),
            ["webapi.115.com/user/fingerprint"] = new("/user/fingerprint", "code", ErrorCodeType.String, "message", null, "msg"),
            ["pay.115.com/"] = new("/", "code", ErrorCodeType.Number, "msg", null, "message"),
            ["webapi.115.com/share/send"] = new("/share/send", "errNo", ErrorCodeType.Number, "error", "errno"),
            ["webapi.115.com/share/receive"] = new("/share/receive", "errNo", ErrorCodeType.Number, "error", "errno"),
            ["webapi.115.com/share/slist"] = new("/share/slist", "errno", ErrorCodeType.Number, "error", "errNo"),
            ["115.com/web/lixian/"] = new("/web/lixian/", "errcode", ErrorCodeType.Number, "error_msg", "errno", "error"),
            ["webapi.115.com/rb"] = new("/rb", "errNo", ErrorCodeType.Number, "error", "errno"),
            ["webapi.115.com/files"] = new("/files", "errNo", ErrorCodeType.Number, "error", "errno"),
            ["webapi.115.com/files/add"] = new("/files/add", "errNo", ErrorCodeType.Number, "error", "errno"),
            ["webapi.115.com/files/edit"] = new("/files/edit", "errNo", ErrorCodeType.Number, "error", "errno"),
            ["webapi.115.com/files/video"] = new("/files/video", "errno", ErrorCodeType.Number, "error", "errNo"),
            ["webapi.115.com/files/video_push"] = new("/files/video_push", "errNo", ErrorCodeType.Number, "error", "errno"),
            ["webapi.115.com/movies/transcode"] = new("/movies/transcode", "errNo", ErrorCodeType.Number, "error", "errno"),
            ["webapi.115.com/files/history"] = new("/files/history", "code", ErrorCodeType.String, "error", "errno", "message"),
            ["my.115.com/proapi/"] = new("/proapi/", "code", ErrorCodeType.Number, "message"),
            ["act.115.com/"] = new("/", "code", ErrorCodeType.Number, "message"),
        };

        return descriptors;
    }

    /// <summary>
    /// 根据请求 URI 解析最匹配的错误字段描述符。
    /// </summary>
    private static ErrorFieldDescriptor ResolveDescriptor(Uri requestUri)
    {
        string host = requestUri.Host.ToLowerInvariant();
        string path = requestUri.AbsolutePath;

        string exactKey = TrimTrailingSlash($"{host}{path}");

        // 精确匹配优先于目录前缀匹配，避免 /files/add 被较宽泛的 /files 规则提前截获。
        foreach (KeyValuePair<string, ErrorFieldDescriptor> descriptor in Descriptors)
        {
            string key = TrimTrailingSlash(descriptor.Key);
            if (exactKey.Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                return descriptor.Value;
            }
        }

        string hostPath = $"{host}{path}";

        // 以斜杠结尾的注册项表示一个接口族，用于覆盖 my/proapi 与 lixian 这类子路径较多的接口。
        foreach (KeyValuePair<string, ErrorFieldDescriptor> descriptor in Descriptors)
        {
            if (descriptor.Key.EndsWith("/", StringComparison.Ordinal)
                && hostPath.StartsWith(descriptor.Key, StringComparison.OrdinalIgnoreCase))
            {
                return descriptor.Value;
            }
        }

        // 未登记接口仍使用默认字段兜底，让新增接口先具备基本错误识别能力。
        return new ErrorFieldDescriptor(
            "*",
            "code",
            ErrorCodeType.Number,
            "message",
            "errno",
            "error");
    }

    /// <summary>
    /// 移除路径末尾的单个斜杠，便于进行路径等价匹配。
    /// </summary>
    private static string TrimTrailingSlash(string value)
    {
        return value.EndsWith("/", StringComparison.Ordinal)
            ? value.Substring(0, value.Length - 1)
            : value;
    }

    /// <summary>
    /// 使用指定错误字段描述符解析 115 业务错误。
    /// </summary>
    private static CloudDriveApiError? TryParseWithDescriptor(
        Uri requestUri,
        JsonDocument responseJson,
        ErrorFieldDescriptor descriptor)
    {
        string? codeText = ReadString(responseJson, descriptor.CodeFieldName)
            ?? ReadString(responseJson, descriptor.FallbackCodeFieldName);
        string? message = ReadString(responseJson, descriptor.MessageFieldName)
            ?? ReadString(responseJson, descriptor.FallbackMessageFieldName)
            ?? ReadString(responseJson, "Message")
            ?? ReadString(responseJson, "err")
            ?? ReadString(responseJson, "msg")
            ?? ReadString(responseJson, "error");

        bool hasStateFailure = IsStateFailure(responseJson);

        // 没有 state=false 且错误码是成功值时视为正常响应，避免 code=0 被误报为业务错误。
        if (!hasStateFailure && IsSuccessCode(codeText))
        {
            return null;
        }

        // 某些成功响应没有 code/message 字段，此时不能仅因为描述符存在就构造错误。
        if (!hasStateFailure && string.IsNullOrWhiteSpace(codeText) && string.IsNullOrWhiteSpace(message))
        {
            return null;
        }

        string rawField = ResolveRawField(responseJson, descriptor);
        int? numericCode = TryReadNumericCode(codeText);
        return new CloudDriveApiError(numericCode, message ?? codeText, rawField, requestUri);
    }

    /// <summary>
    /// 解析当前错误命中的原始字段名称。
    /// </summary>
    private static string ResolveRawField(JsonDocument responseJson, ErrorFieldDescriptor descriptor)
    {
        if (HasProperty(responseJson, descriptor.CodeFieldName))
        {
            return descriptor.CodeFieldName;
        }

        if (!string.IsNullOrWhiteSpace(descriptor.FallbackCodeFieldName)
            && HasProperty(responseJson, descriptor.FallbackCodeFieldName))
        {
            return descriptor.FallbackCodeFieldName;
        }

        if (HasProperty(responseJson, descriptor.MessageFieldName))
        {
            return descriptor.MessageFieldName;
        }

        // 保留特殊字段名，便于日志和测试追踪是哪类未知结构触发了兜底逻辑。
        return "unknown_error_shape";
    }

    /// <summary>
    /// 判断 state 字段是否表示业务失败。
    /// </summary>
    private static bool IsStateFailure(JsonDocument responseJson)
    {
        if (!responseJson.RootElement.TryGetProperty("state", out JsonElement stateElement))
        {
            return false;
        }

        bool? state = JsonElementReaders.ReadBoolean(stateElement);
        return state == false;
    }

    /// <summary>
    /// 判断响应是否包含任何默认错误字段。
    /// </summary>
    private static bool HasAnyDefaultErrorField(JsonDocument responseJson)
    {
        return HasProperty(responseJson, "code")
            || HasProperty(responseJson, "errno")
            || HasProperty(responseJson, "errNo")
            || HasProperty(responseJson, "errCode")
            || HasProperty(responseJson, "errcode")
            || HasProperty(responseJson, "message")
            || HasProperty(responseJson, "Message")
            || HasProperty(responseJson, "error")
            || HasProperty(responseJson, "err")
            || HasProperty(responseJson, "msg")
            || HasProperty(responseJson, "error_msg");
    }

    /// <summary>
    /// 判断响应 JSON 是否包含指定属性。
    /// </summary>
    private static bool HasProperty(JsonDocument responseJson, string? propertyName)
    {
        return !string.IsNullOrWhiteSpace(propertyName)
            && responseJson.RootElement.TryGetProperty(propertyName, out _);
    }

    /// <summary>
    /// 从响应 JSON 的指定属性中读取字符串值。
    /// </summary>
    private static string? ReadString(JsonDocument responseJson, string? propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName)
            || !responseJson.RootElement.TryGetProperty(propertyName, out JsonElement property))
        {
            return null;
        }

        return JsonElementReaders.ReadString(property);
    }

    /// <summary>
    /// 判断错误码文本是否表示业务成功。
    /// </summary>
    private static bool IsSuccessCode(string? codeText)
    {
        if (string.IsNullOrWhiteSpace(codeText))
        {
            return true;
        }

        return codeText.Equals("0", StringComparison.OrdinalIgnoreCase)
            || codeText.Equals("success", StringComparison.OrdinalIgnoreCase)
            || codeText.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 尝试将错误码文本读取为数字错误码。
    /// </summary>
    private static int? TryReadNumericCode(string? codeText)
    {
        if (string.IsNullOrWhiteSpace(codeText))
        {
            return null;
        }

        return int.TryParse(
            codeText,
            System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture,
            out int code)
            ? code
            : null;
    }
}
