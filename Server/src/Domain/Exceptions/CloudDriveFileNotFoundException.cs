namespace Domain.Exceptions;

/// <summary>
/// 115 网盘文件或目录不存在时抛出。
/// </summary>
public sealed class CloudDriveFileNotFoundException : DomainException
{
    /// <summary>
    /// 创建文件不存在异常。
    /// </summary>
    /// <param name="message">错误消息。</param>
    /// <param name="errorCode">业务错误码。</param>
    public CloudDriveFileNotFoundException(
        string message = "115 网盘文件不存在",
        string? errorCode = "cloud_drive_file_not_found")
        : base(404, message, errorCode)
    {
    }

    private CloudDriveFileNotFoundException(int statusCode, string message, string? errorCode = null) : base(statusCode, message, errorCode)
    {
    }

    private CloudDriveFileNotFoundException(int statusCode, string message, Exception innerException, string? errorCode = null) : base(statusCode, message, innerException, errorCode)
    {
    }
}
