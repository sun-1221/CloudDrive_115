namespace Domain.Exceptions;

/// <summary>
/// 115 网盘上传初始化、直传或完成确认失败时抛出。
/// </summary>
public sealed class CloudDriveUploadException : DomainException
{
    /// <summary>
    /// 创建上传异常。
    /// </summary>
    /// <param name="message">错误消息。</param>
    /// <param name="errorCode">业务错误码。</param>
    public CloudDriveUploadException(
        string message = "115 网盘文件上传失败",
        string? errorCode = "cloud_drive_upload_failed")
        : base(500, message, errorCode)
    {
    }
}
