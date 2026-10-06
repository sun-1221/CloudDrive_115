using Domain.Entities;

namespace Application.Abstractions.CloudDrive;

/// <summary>
/// 115 网盘后台任务审计日志抽象。实现必须在写日志前执行敏感字段脱敏。
/// </summary>
public interface ICloudDriveJobAuditLogger
{
    /// <summary>
    /// 记录后台任务已创建。
    /// </summary>
    void LogCreated(CloudDriveJob job);

    /// <summary>
    /// 记录后台任务状态或进度已更新。
    /// </summary>
    void LogState(CloudDriveJob job, string action);

    /// <summary>
    /// 记录后台任务错误摘要。
    /// </summary>
    void LogError(CloudDriveJob job, Exception exception);
}
