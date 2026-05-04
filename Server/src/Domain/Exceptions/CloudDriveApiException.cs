namespace Domain.Exceptions;

/// <summary>
/// 115 网页端 API 返回未分类业务错误或未知错误响应结构时抛出。
/// </summary>
public sealed class CloudDriveApiException : DomainException
{
    /// <summary>
    /// 创建通用 API 异常。
    /// </summary>
    /// <param name="message">错误消息。</param>
    /// <param name="errorCode">业务错误码。</param>
    public CloudDriveApiException(
        string message = "115 网盘 API 调用失败",
        string? errorCode = "cloud_drive_api_error")
        : base(502, message, errorCode)
    {
    }

    /// <summary>
    /// 创建包含上游错误字段的通用 API 异常。
    /// </summary>
    /// <param name="message">错误消息。</param>
    /// <param name="errorCode">业务错误码。</param>
    /// <param name="upstreamCode">115 上游错误码。</param>
    /// <param name="upstreamMessage">115 上游错误消息。</param>
    public CloudDriveApiException(
        string message,
        string? errorCode,
        string? upstreamCode,
        string? upstreamMessage)
        : base(502, message, errorCode)
    {
        UpstreamCode = upstreamCode;
        UpstreamMessage = upstreamMessage;
    }

    /// <summary>
    /// 创建包含内部异常的通用 API 异常。
    /// </summary>
    /// <param name="message">错误消息。</param>
    /// <param name="innerException">内部异常。</param>
    /// <param name="errorCode">业务错误码。</param>
    public CloudDriveApiException(
        string message,
        Exception innerException,
        string? errorCode = "cloud_drive_api_error")
        : base(502, message, innerException, errorCode)
    {
    }

    /// <summary>
    /// 获取 115 上游错误码。
    /// </summary>
    public string? UpstreamCode { get; }

    /// <summary>
    /// 获取 115 上游错误消息。
    /// </summary>
    public string? UpstreamMessage { get; }
}
