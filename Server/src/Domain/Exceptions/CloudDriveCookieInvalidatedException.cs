namespace Domain.Exceptions;

/// <summary>
/// 115 Cookie 已失效、登录态无效或被服务端拒绝时抛出。
/// </summary>
public sealed class CloudDriveCookieInvalidatedException : DomainException
{
    /// <summary>
    /// 创建 Cookie 失效异常。
    /// </summary>
    /// <param name="message">错误消息。</param>
    /// <param name="errorCode">业务错误码。</param>
    public CloudDriveCookieInvalidatedException(
        string message = "115 Cookie 会话已失效",
        string? errorCode = "cloud_drive_cookie_invalidated")
        : base(401, message, errorCode)
    {
    }

    /// <summary>
    /// 创建包含内部异常的 Cookie 失效异常。
    /// </summary>
    /// <param name="message">错误消息。</param>
    /// <param name="innerException">内部异常。</param>
    /// <param name="errorCode">业务错误码。</param>
    public CloudDriveCookieInvalidatedException(
        string message,
        Exception innerException,
        string? errorCode = "cloud_drive_cookie_invalidated")
        : base(401, message, innerException, errorCode)
    {
    }
}
