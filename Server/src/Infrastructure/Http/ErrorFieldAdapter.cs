using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;

using Application.Abstractions.CloudDrive;

namespace Infrastructure.Http;

/// <summary>
/// 115 网页端错误字段适配器。按 method + host + 精确 path + observed query 匹配规则。
/// </summary>
internal sealed class ErrorFieldAdapter : IErrorFieldAdapter
{
    private static readonly IReadOnlyDictionary<string, ErrorFieldDescriptor> Descriptors =
        new ReadOnlyDictionary<string, ErrorFieldDescriptor>(CreateDescriptors());

    /// <inheritdoc />
    public CloudDriveApiError? TryParse(HttpMethod method, Uri requestUri, JsonDocument responseJson)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(requestUri);
        ArgumentNullException.ThrowIfNull(responseJson);

        bool stateKnown = TryReadState(responseJson, out bool state);
        if (stateKnown && state)
        {
            return null;
        }

        Resolution resolution = ResolveDescriptor(method, requestUri);
        if (!resolution.Matched || resolution.Ambiguous)
        {
            return (stateKnown && !state) || (!stateKnown && HasAnyErrorLikeField(responseJson))
                ? UnknownError(requestUri)
                : null;
        }

        ErrorFieldDescriptor descriptor = resolution.Descriptor!;
        if (!stateKnown)
        {
            // 所有已采集 envelope 的 state 均为 boolean；类型缺失或变化必须 fail closed。
            return HasRegisteredErrorField(responseJson, descriptor) ? UnknownError(requestUri) : null;
        }

        return state ? null : TryParseFailure(requestUri, responseJson, descriptor);
    }

    /// <summary>
    /// 获取已注册的错误描述符，供契约测试核对证据闭集。
    /// </summary>
    internal static IReadOnlyDictionary<string, ErrorFieldDescriptor> RegisteredDescriptors => Descriptors;

    private static Dictionary<string, ErrorFieldDescriptor> CreateDescriptors()
    {
        var descriptors = new Dictionary<string, ErrorFieldDescriptor>(StringComparer.Ordinal)
        {
            [Key("GET", "webapi.115.com", "/files")] = Descriptor("GET", "webapi.115.com", "/files", "errNo", ErrorCodeType.Number, "error"),
            [Key("POST", "webapi.115.com", "/files/add")] = Descriptor("POST", "webapi.115.com", "/files/add", "errno", ErrorCodeType.String, "error"),
            [Key("POST", "webapi.115.com", "/files/batch_rename")] = Descriptor("POST", "webapi.115.com", "/files/batch_rename", "errno", ErrorCodeType.Number, "error"),
            [Key("POST", "webapi.115.com", "/files/copy")] = Descriptor("POST", "webapi.115.com", "/files/copy", "errno", ErrorCodeType.String, "error"),
            [Key("POST", "webapi.115.com", "/files/star")] = Descriptor("POST", "webapi.115.com", "/files/star", null, null, "error"),
            [Key("GET", "webapi.115.com", "/files/download")] = Descriptor("GET", "webapi.115.com", "/files/download", "msg_code", ErrorCodeType.Number, "msg"),
            [Key("GET", "webapi.115.com", "/files/video")] = Descriptor("GET", "webapi.115.com", "/files/video", "errno", ErrorCodeType.Number, "error"),
            [Key("GET", "webapi.115.com", "/movies/subtitle")] = Descriptor("GET", "webapi.115.com", "/movies/subtitle", "errNo", ErrorCodeType.Number, "error"),
            [Key("GET", "webapi.115.com", "/rb")] = Descriptor("GET", "webapi.115.com", "/rb", null, null, "error"),
            [Key("GET", "webapi.115.com", "/share/slist")] = Descriptor("GET", "webapi.115.com", "/share/slist", "errno", ErrorCodeType.Number, "error"),
            [Key("GET", "webapi.115.com", "/share/send")] = DescriptorWithFallback("GET", "webapi.115.com", "/share/send", "errNo", ErrorCodeType.Number, "error", "errno"),
            [Key("GET", "webapi.115.com", "/share/receive")] = DescriptorWithFallback("GET", "webapi.115.com", "/share/receive", "errNo", ErrorCodeType.Number, "error", "errno"),
            [Key("GET", "webapi.115.com", "/user/vip_limit", ("feature", "2"))] = Descriptor("GET", "webapi.115.com", "/user/vip_limit", "code", ErrorCodeType.String, "message", ("feature", "2")),
            [Key("GET", "webapi.115.com", "/files/index_info", ("count_space_nums", "1"))] = Descriptor("GET", "webapi.115.com", "/files/index_info", "code", ErrorCodeType.Number, "error", ("count_space_nums", "1")),
            [Key("GET", "webapi.115.com", "/user/setting", ("format", "json"))] = Descriptor("GET", "webapi.115.com", "/user/setting", "errno", ErrorCodeType.Number, "error", ("format", "json")),
            [Key("GET", "pay.115.com", "/", ("ct", "balance"), ("ac", "getBalance"))] = Descriptor("GET", "pay.115.com", "/", "code", ErrorCodeType.Number, "msg", ("ct", "balance"), ("ac", "getBalance")),
            [Key("GET", "vip.115.com", "/", ("ct", "order"), ("ac", "get_yun_card"))] = Descriptor("GET", "vip.115.com", "/", null, null, null, ("ct", "order"), ("ac", "get_yun_card")),
            [Key("GET", "vip.115.com", "/", ("ct", "product"), ("ac", "product_list"))] = Descriptor("GET", "vip.115.com", "/", "code", ErrorCodeType.Number, "msg", ("ct", "product"), ("ac", "product_list")),
            [Key("GET", "vip.115.com", "/", ("ct", "act_coupon"), ("ac", "get_coupon_yhq_list"))] = Descriptor("GET", "vip.115.com", "/", "code", ErrorCodeType.Number, null, ("ct", "act_coupon"), ("ac", "get_coupon_yhq_list")),
            [Key("POST", "clouddownload.115.com", "/web/", ("ac", "task_lists"))] = DescriptorWithMessageFallback("POST", "clouddownload.115.com", "/web/", "errcode", ErrorCodeType.Number, "errtype", "error_msg", ("ac", "task_lists")),
            [Key("POST", "clouddownload.115.com", "/web/", ("ac", "task_count"))] = DescriptorWithMessageFallback("POST", "clouddownload.115.com", "/web/", "errcode", ErrorCodeType.Number, "errtype", "error_msg", ("ac", "task_count")),
            [Key("POST", "clouddownload.115.com", "/web/", ("ac", "get_quota_package_info"))] = DescriptorWithMessageFallback("POST", "clouddownload.115.com", "/web/", "errcode", ErrorCodeType.Number, "errtype", "error_msg", ("ac", "get_quota_package_info")),
            [Key("POST", "clouddownload.115.com", "/web/", ("ac", "add_task_urls"))] = DescriptorWithFallback("POST", "clouddownload.115.com", "/web/", "errcode", ErrorCodeType.Number, "error_msg", "errno", ("ac", "add_task_urls")),
            [Key("GET", "webapi.115.com", "/files/history")] = Descriptor("GET", "webapi.115.com", "/files/history", null, null, "error"),
        };

        return descriptors;
    }

    private static ErrorFieldDescriptor Descriptor(
        string method,
        string host,
        string path,
        string? codeFieldName,
        ErrorCodeType? codeType,
        string? messageFieldName,
        params (string Name, string Value)[] query)
    {
        IReadOnlyDictionary<string, string>? requiredQuery = query.Length == 0
            ? null
            : query.ToDictionary(item => item.Name, item => item.Value, StringComparer.OrdinalIgnoreCase);

        return new ErrorFieldDescriptor(method, host, path, codeFieldName, codeType, messageFieldName, requiredQuery);
    }

    private static ErrorFieldDescriptor DescriptorWithFallback(
        string method,
        string host,
        string path,
        string? codeFieldName,
        ErrorCodeType? codeType,
        string? messageFieldName,
        string fallbackCodeFieldName,
        params (string Name, string Value)[] query)
    {
        ErrorFieldDescriptor descriptor = Descriptor(method, host, path, codeFieldName, codeType, messageFieldName, query);
        return descriptor with { FallbackCodeFieldName = fallbackCodeFieldName };
    }

    private static ErrorFieldDescriptor DescriptorWithMessageFallback(
        string method,
        string host,
        string path,
        string? codeFieldName,
        ErrorCodeType? codeType,
        string? messageFieldName,
        string fallbackMessageFieldName,
        params (string Name, string Value)[] query)
    {
        ErrorFieldDescriptor descriptor = Descriptor(method, host, path, codeFieldName, codeType, messageFieldName, query);
        return descriptor with { FallbackMessageFieldName = fallbackMessageFieldName };
    }

    private static string Key(string method, string host, string path, params (string Name, string Value)[] query) =>
        query.Length == 0
            ? $"{method} {host}{path}"
            : $"{method} {host}{path}?{string.Join('&', query.Select(item => $"{item.Name}={item.Value}"))}";

    private static Resolution ResolveDescriptor(HttpMethod method, Uri requestUri)
    {
        string methodName = method.Method.ToUpperInvariant();
        string host = requestUri.Host.ToLowerInvariant();
        IReadOnlyDictionary<string, string> queryValues = ParseQuery(requestUri.Query);

        List<ErrorFieldDescriptor> candidates = Descriptors.Values
            .Where(descriptor => descriptor.Method.Equals(methodName, StringComparison.Ordinal)
                && descriptor.Host.Equals(host, StringComparison.OrdinalIgnoreCase)
                && descriptor.UrlPattern.Equals(requestUri.AbsolutePath, StringComparison.Ordinal)
                && QueryMatches(queryValues, descriptor.RequiredQueryParameters))
            .OrderByDescending(descriptor => descriptor.UrlPattern.Length)
            .ThenByDescending(descriptor => descriptor.RequiredQueryParameters?.Count ?? 0)
            .ToList();

        if (candidates.Count == 0)
        {
            return new Resolution(null, false, false);
        }

        int specificity = candidates[0].RequiredQueryParameters?.Count ?? 0;
        bool ambiguous = candidates.Count(item => item.UrlPattern.Length == candidates[0].UrlPattern.Length
            && (item.RequiredQueryParameters?.Count ?? 0) == specificity) > 1;
        return new Resolution(candidates[0], true, ambiguous);
    }

    private static bool QueryMatches(IReadOnlyDictionary<string, string> query, IReadOnlyDictionary<string, string>? required)
    {
        if (required is null)
        {
            return true;
        }

        return required.All(item => query.TryGetValue(item.Key, out string? value)
            && value.Equals(item.Value, StringComparison.Ordinal));
    }

    private static IReadOnlyDictionary<string, string> ParseQuery(string queryString)
    {
        var query = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string rawQuery = queryString.TrimStart('?');
        foreach (string part in rawQuery.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] pair = part.Split('=', 2);
            string name = Uri.UnescapeDataString(pair[0].Replace('+', ' '));
            string value = pair.Length == 1 ? string.Empty : Uri.UnescapeDataString(pair[1].Replace('+', ' '));
            query[name] = value;
        }

        return query;
    }

    private static CloudDriveApiError? TryParseFailure(Uri requestUri, JsonDocument responseJson, ErrorFieldDescriptor descriptor)
    {
        (string? Value, string? Field) code = ReadCode(responseJson, descriptor.CodeFieldName, descriptor.CodeType);
        if (code.Value is null)
        {
            code = ReadCode(responseJson, descriptor.FallbackCodeFieldName, descriptor.CodeType);
        }

        string? codeText = code.Value;
        string? message = ReadMessage(responseJson, descriptor.MessageFieldName)
            ?? ReadMessage(responseJson, descriptor.FallbackMessageFieldName);

        if (codeText is null && message is null)
        {
            return UnknownError(requestUri);
        }

        string rawField = ResolveRawField(responseJson, descriptor, code.Field, message);
        int? numericCode = int.TryParse(codeText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
            ? parsed
            : null;
        return new CloudDriveApiError(numericCode, message ?? codeText, rawField, requestUri);
    }

    private static (string? Value, string? Field) ReadCode(JsonDocument document, string? fieldName, ErrorCodeType? codeType)
    {
        if (string.IsNullOrWhiteSpace(fieldName)
            || codeType is null
            || !document.RootElement.TryGetProperty(fieldName, out JsonElement property))
        {
            return (null, null);
        }

        string? value = codeType == ErrorCodeType.Number && property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out int number)
            ? number.ToString(CultureInfo.InvariantCulture)
            : codeType == ErrorCodeType.String && property.ValueKind == JsonValueKind.String
                ? property.GetString()
                : null;
        return (value, value is null ? null : fieldName);
    }

    private static string? ReadMessage(JsonDocument document, string? fieldName)
    {
        return string.IsNullOrWhiteSpace(fieldName)
            || !document.RootElement.TryGetProperty(fieldName, out JsonElement property)
            || property.ValueKind != JsonValueKind.String
            ? null
            : property.GetString();
    }

    private static string ResolveRawField(JsonDocument document, ErrorFieldDescriptor descriptor, string? codeField, string? message)
    {
        if (!string.IsNullOrWhiteSpace(codeField))
        {
            return codeField;
        }

        if (message is not null && !string.IsNullOrWhiteSpace(descriptor.MessageFieldName)
            && document.RootElement.TryGetProperty(descriptor.MessageFieldName, out _))
        {
            return descriptor.MessageFieldName;
        }

        if (message is not null && !string.IsNullOrWhiteSpace(descriptor.FallbackMessageFieldName))
        {
            return descriptor.FallbackMessageFieldName;
        }

        return "unknown_error_shape";
    }

    private static bool HasRegisteredErrorField(JsonDocument document, ErrorFieldDescriptor descriptor)
    {
        return (!string.IsNullOrWhiteSpace(descriptor.CodeFieldName) && document.RootElement.TryGetProperty(descriptor.CodeFieldName, out _))
            || (!string.IsNullOrWhiteSpace(descriptor.MessageFieldName) && document.RootElement.TryGetProperty(descriptor.MessageFieldName, out _));
    }

    private static bool HasAnyErrorLikeField(JsonDocument document)
    {
        string[] fields = ["code", "errno", "errNo", "errCode", "errcode", "msg_code", "error", "error_msg", "message", "msg"];
        return fields.Any(field => document.RootElement.TryGetProperty(field, out _));
    }

    private static bool TryReadState(JsonDocument document, out bool state)
    {
        if (document.RootElement.TryGetProperty("state", out JsonElement value)
            && (value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False))
        {
            state = value.GetBoolean();
            return true;
        }

        state = false;
        return false;
    }

    private static CloudDriveApiError UnknownError(Uri requestUri) =>
        new(null, null, "unknown_error_shape", requestUri);

    private sealed record Resolution(ErrorFieldDescriptor? Descriptor, bool Matched, bool Ambiguous);
}
