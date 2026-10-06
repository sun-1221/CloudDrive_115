namespace Domain.Enums;

/// <summary>
/// 115 网盘后台任务执行状态。
/// </summary>
public enum CloudDriveJobStatus
{
    /// <summary>任务已创建，等待调度执行。</summary>
    Pending = 0,

    /// <summary>任务正在执行中。</summary>
    Running = 1,

    /// <summary>任务已成功完成。</summary>
    Completed = 2,

    /// <summary>任务执行失败。</summary>
    Failed = 3,

    /// <summary>任务已被取消。</summary>
    Cancelled = 4,
}
