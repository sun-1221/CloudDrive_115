namespace Domain.Exceptions;

/// <summary>
/// 115 网盘集成功能配置缺失或被禁用时抛出。
/// </summary>
public sealed class CloudDriveConfigurationException : DomainException
{
    /// <summary>
    /// 创建配置异常。
    /// </summary>
    /// <param name="message">错误消息。</param>
    /// <param name="errorCode">业务错误码。</param>
    public CloudDriveConfigurationException(
        string message = "115 网盘集成功能配置不可用",
        string? errorCode = "cloud_drive_configuration_unavailable")
        : base(503, message, errorCode)
    {
    }

    private CloudDriveConfigurationException(int statusCode, string message, string? errorCode = null) : base(statusCode, message, errorCode)
    {
    }

    private CloudDriveConfigurationException(int statusCode, string message, Exception innerException, string? errorCode = null) : base(statusCode, message, innerException, errorCode)
    {
    }
}
