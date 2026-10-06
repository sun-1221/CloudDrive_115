using System.Text.Json;

namespace Application.Abstractions.CloudDrive;

/// <summary>
/// 115 网页端错误响应字段适配器抽象。覆盖 12 种错误响应字段变体
/// （errNo / errCode / errno / code / msg / message 等），按 method + host + path + query 声明具体映射规则。
/// </summary>
/// <remarks>
/// 定义在 Application 层，基础设施层由 <c>ErrorFieldAdapter</c> 实现。
/// </remarks>
public interface IErrorFieldAdapter
{
    /// <summary>
    /// 根据请求 method、URL 与响应 JSON 解析出标准化的错误视图。
    /// 返回 <c>null</c> 表示响应无错误或不匹配任何已知错误模式。
    /// </summary>
    /// <returns>返回当前操作的处理结果。</returns>
    CloudDriveApiError? TryParse(HttpMethod method, Uri requestUri, JsonDocument responseJson);
}

/// <summary>
/// 标准化的 115 API 错误视图。
/// </summary>
/// <param name="Code">错误码（数字型），可能为 <c>null</c>（纯字符串型错误码场景）。</param>
/// <param name="Message">错误消息。</param>
/// <param name="RawField">原始错误字段名称（如 errNo、errCode、errno 等）。</param>
/// <param name="SourceUri">产生该错误的请求 URI。</param>
public sealed record CloudDriveApiError(int? Code, string? Message, string RawField, Uri SourceUri);

/// <summary>
/// 单条 URL 对应的错误字段解析规则。
/// </summary>
/// <param name="Method">HTTP 方法。</param>
/// <param name="Host">目标主机。</param>
/// <param name="UrlPattern">URL 路径模式（如 /files/search）。</param>
/// <param name="CodeFieldName">错误码字段名称（如 errCode）。</param>
/// <param name="CodeType">错误码类型（Number / String）。</param>
/// <param name="MessageFieldName">错误消息字段名称（如 errMsg、error）。</param>
/// <param name="RequiredQueryParameters">规则要求的 observed query 参数。</param>
/// <param name="FallbackCodeFieldName">备选错误码字段（如 errNo 与 errno 双写场景）。</param>
/// <param name="FallbackMessageFieldName">备选错误消息字段。</param>
public sealed record ErrorFieldDescriptor(
    string Method,
    string Host,
    string UrlPattern,
    string? CodeFieldName,
    ErrorCodeType? CodeType,
    string? MessageFieldName,
    IReadOnlyDictionary<string, string>? RequiredQueryParameters = null,
    string? FallbackCodeFieldName = null,
    string? FallbackMessageFieldName = null);

/// <summary>
/// 错误码字段值类型。
/// </summary>
public enum ErrorCodeType
{
    /// <summary>
    /// 数值型错误码。
    /// </summary>
    Number,

    /// <summary>
    /// 字符串型错误码。
    /// </summary>
    String,
}
