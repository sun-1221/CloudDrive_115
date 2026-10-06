namespace Domain.Exceptions;

/// <summary>
/// 调用 115 网页端 API 出现网络超时或通信失败时抛出。
/// </summary>
public sealed class CloudDriveNetworkException : DomainException
{
    /// <summary>
    /// 创建网络异常。
    /// </summary>
    /// <param name="message">错误消息。</param>
    /// <param name="errorCode">业务错误码。</param>
    public CloudDriveNetworkException(
        string message = "115 网盘服务通信失败",
        string? errorCode = "cloud_drive_network_error")
        : base(502, message, errorCode)
    {
    }

    /// <summary>
    /// 创建包含内部异常的网络异常。
    /// </summary>
    /// <param name="message">错误消息。</param>
    /// <param name="innerException">内部异常。</param>
    /// <param name="errorCode">业务错误码。</param>
    public CloudDriveNetworkException(
        string message,
        Exception innerException,
        string? errorCode = "cloud_drive_network_error")
        : base(502, message, innerException, errorCode)
    {
    }

    private CloudDriveNetworkException(int statusCode, string message, string? errorCode = null) : base(statusCode, message, errorCode)
    {
    }

    private CloudDriveNetworkException(int statusCode, string message, Exception innerException, string? errorCode = null) : base(statusCode, message, innerException, errorCode)
    {
    }
}
