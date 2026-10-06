namespace Domain.Exceptions;

/// <summary>
/// 115 网盘文件名或文件夹名非法时抛出。
/// </summary>
public sealed class CloudDriveInvalidNameException : DomainException
{
    /// <summary>
    /// 创建名称非法异常。
    /// </summary>
    /// <param name="message">错误消息。</param>
    /// <param name="errorCode">业务错误码。</param>
    public CloudDriveInvalidNameException(
        string message = "115 网盘文件名非法",
        string? errorCode = "cloud_drive_invalid_name")
        : base(400, message, errorCode)
    {
    }

    private CloudDriveInvalidNameException(int statusCode, string message, string? errorCode = null) : base(statusCode, message, errorCode)
    {
    }

    private CloudDriveInvalidNameException(int statusCode, string message, Exception innerException, string? errorCode = null) : base(statusCode, message, innerException, errorCode)
    {
    }
}
