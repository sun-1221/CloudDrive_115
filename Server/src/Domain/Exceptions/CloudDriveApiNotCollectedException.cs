namespace Domain.Exceptions;

/// <summary>
/// 115 网页端能力尚未完成证据采集或实现时抛出。
/// </summary>
public sealed class CloudDriveApiNotCollectedException : DomainException
{
    /// <summary>
    /// 创建未采集能力异常。
    /// </summary>
    /// <param name="message">面向日志和 ProblemDetails 的脱敏消息。</param>
    public CloudDriveApiNotCollectedException(
        string message = "115 网页端接口尚未采集，当前能力保持禁用。")
        : base(501, message, "cloud_drive_api_not_collected")
    {
    }
}
