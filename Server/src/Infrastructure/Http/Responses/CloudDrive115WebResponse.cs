using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

/// <summary>
/// 115 网页端 API 的通用响应包装；错误字段保留所有已观察到的别名，
/// 由 <c>IErrorFieldAdapter</c> 按 URL 路径选择权威字段。
/// </summary>
internal sealed class CloudDrive115WebResponse<T>
{
    /// <summary>
    /// 获取或设置 115 响应的业务成功状态。
    /// </summary>
    [JsonConverter(typeof(BoolOrNumberJsonConverter))]
    [JsonPropertyName("state")]
    public bool State { get; set; }

    /// <summary>
    /// 获取或设置 115 响应的数据字段。
    /// </summary>
    [JsonPropertyName("data")]
    public T? Data { get; set; }

    /// <summary>
    /// 获取或设置 115 响应的小写 message 消息字段。
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// 获取或设置 115 响应的大写 Message 消息字段。
    /// </summary>
    [JsonPropertyName("Message")]
    public string? UpperMessage { get; set; }

    /// <summary>
    /// 获取或设置 115 响应的 msg 消息字段。
    /// </summary>
    [JsonPropertyName("msg")]
    public string? Msg { get; set; }

    /// <summary>
    /// 获取或设置 115 响应的 error 消息字段。
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>
    /// 获取或设置 115 响应的 err 消息字段。
    /// </summary>
    [JsonPropertyName("err")]
    public string? Err { get; set; }

    /// <summary>
    /// 获取或设置 115 响应的 code 错误码字段。
    /// </summary>
    [JsonConverter(typeof(StringOrNumberJsonConverter))]
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    /// <summary>
    /// 获取或设置 115 响应的 errNo 错误码字段。
    /// </summary>
    [JsonConverter(typeof(StringOrNumberJsonConverter))]
    [JsonPropertyName("errNo")]
    public string? ErrNo { get; set; }

    /// <summary>
    /// 获取或设置 115 响应的 errno 错误码字段。
    /// </summary>
    [JsonConverter(typeof(StringOrNumberJsonConverter))]
    [JsonPropertyName("errno")]
    public string? Errno { get; set; }

    /// <summary>
    /// 获取或设置 115 响应的 errCode 错误码字段。
    /// </summary>
    [JsonConverter(typeof(StringOrNumberJsonConverter))]
    [JsonPropertyName("errCode")]
    public string? ErrCode { get; set; }

    /// <summary>
    /// 获取或设置 115 响应的大写 ErrNo 错误码字段。
    /// </summary>
    [JsonConverter(typeof(StringOrNumberJsonConverter))]
    [JsonPropertyName("ErrNo")]
    public string? UpperErrNo { get; set; }

    /// <summary>
    /// 获取或设置 115 响应回显的请求信息。
    /// </summary>
    [JsonPropertyName("request")]
    public string? Request { get; set; }
}
