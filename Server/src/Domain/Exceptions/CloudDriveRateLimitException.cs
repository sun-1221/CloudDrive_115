namespace Domain.Exceptions;

/// <summary>
/// 115 网盘请求触发本地或远端频率限制时抛出。
/// </summary>
public sealed class CloudDriveRateLimitException : DomainException
{
    /// <summary>
    /// 创建频率限制异常。
    /// </summary>
    /// <param name="message">错误消息。</param>
    /// <param name="errorCode">业务错误码。</param>
    public CloudDriveRateLimitException(
        string message = "115 网盘请求过于频繁",
        string? errorCode = "cloud_drive_rate_limited")
        : base(429, message, errorCode)
    {
    }
}
