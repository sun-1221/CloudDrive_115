using System.Net.Http.Json;
using System.Text.Json;

namespace Infrastructure.Http;

/// <summary>
/// HTTP 请求构造辅助方法。为业务模块提供 query string、
/// <c>application/x-www-form-urlencoded</c>、<c>multipart/form-data</c>
/// 与 JSON 四类请求的统一扩展点（R9.5）。
/// </summary>
public static class HttpRequestBuilders
{
    /// <summary>
    /// 构造携带 query string 的 GET 请求。
    /// </summary>
    /// <param name="requestUri">相对或绝对 URI。</param>
    /// <param name="queryParameters">键值对；<see langword="null"/> 值将被忽略。</param>
    public static HttpRequestMessage CreateGet(
        string requestUri,
        IReadOnlyDictionary<string, string?>? queryParameters = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestUri);

        string finalUri = AppendQueryString(requestUri, queryParameters);
        return new HttpRequestMessage(HttpMethod.Get, finalUri);
    }

    /// <summary>
    /// 构造 <c>application/x-www-form-urlencoded</c> 请求。
    /// </summary>
    public static HttpRequestMessage CreateFormUrlEncoded(
        HttpMethod method,
        string requestUri,
        IEnumerable<KeyValuePair<string, string>> formFields)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestUri);
        ArgumentNullException.ThrowIfNull(formFields);

        return new HttpRequestMessage(method, requestUri)
        {
            Content = new FormUrlEncodedContent(formFields),
        };
    }

    /// <summary>
    /// 构造 <c>multipart/form-data</c> 请求。
    /// </summary>
    /// <param name="method">HTTP 方法。</param>
    /// <param name="requestUri">相对或绝对 URI。</param>
    /// <param name="configure">用于追加文本 / 文件部分的回调。</param>
    public static HttpRequestMessage CreateMultipartForm(
        HttpMethod method,
        string requestUri,
        Action<MultipartFormDataContent> configure)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestUri);
        ArgumentNullException.ThrowIfNull(configure);

        var content = new MultipartFormDataContent();
        configure(content);

        return new HttpRequestMessage(method, requestUri) { Content = content };
    }

    /// <summary>
    /// 构造 JSON 请求。
    /// </summary>
    /// <typeparam name="T">请求体类型。</typeparam>
    /// <param name="method">HTTP 方法。</param>
    /// <param name="requestUri">相对或绝对 URI。</param>
    /// <param name="body">请求体对象，可为 <see langword="null"/>。</param>
    /// <param name="options">自定义 JSON 序列化选项；<see langword="null"/> 时使用默认值。</param>
    public static HttpRequestMessage CreateJson<T>(
        HttpMethod method,
        string requestUri,
        T body,
        JsonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestUri);

        return new HttpRequestMessage(method, requestUri)
        {
            Content = JsonContent.Create(body, options: options),
        };
    }

    /// <summary>
    /// 将查询参数追加到请求端点。
    /// </summary>
    private static string AppendQueryString(
        string requestUri,
        IReadOnlyDictionary<string, string?>? queryParameters)
    {
        if (queryParameters is null || queryParameters.Count == 0)
        {
            return requestUri;
        }

        List<string> pairs = [];
        foreach (KeyValuePair<string, string?> kv in queryParameters)
        {
            // 空值参数直接忽略，避免把可选参数序列化成 key= 后改变 115 默认行为。
            if (kv.Value is null)
            {
                continue;
            }

            string encodedKey = Uri.EscapeDataString(kv.Key);
            string encodedValue = Uri.EscapeDataString(kv.Value);
            pairs.Add($"{encodedKey}={encodedValue}");
        }

        if (pairs.Count == 0)
        {
            return requestUri;
        }

        string queryString = string.Join('&', pairs);
        char separator = requestUri.Contains('?') ? '&' : '?';
        return $"{requestUri}{separator}{queryString}";
    }
}
