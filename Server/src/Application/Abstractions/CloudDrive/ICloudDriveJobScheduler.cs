using Domain.Enums;

namespace Application.Abstractions.CloudDrive;

/// <summary>
/// 后台任务调度抽象。Infrastructure 层负责将任务 ID 映射到具体 Quartz Trigger。
/// </summary>
public interface ICloudDriveJobScheduler
{
    /// <summary>
    /// 调度指定后台任务。业务载荷由 Job 根据 <paramref name="jobId"/> 从数据库读取。
    /// </summary>
    Task ScheduleAsync(
        Guid jobId,
        CloudDriveJobType jobType,
        CancellationToken cancellationToken = default);
}
