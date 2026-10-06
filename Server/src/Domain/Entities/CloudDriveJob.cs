using Domain.Enums;
using Domain.Events;

namespace Domain.Entities;

/// <summary>
/// 115 网盘后台任务聚合根，作为长耗时操作状态与恢复信息的唯一事实来源。
/// </summary>
/// <remarks>
/// 任务记录保存业务载荷与检查点数据，Quartz 仅承担调度职责，不作为持久化来源。
/// </remarks>
public sealed class CloudDriveJob : AggregateRoot<Guid>
{
    /// <summary>
    /// ORM 反序列化专用。业务代码请使用 <see cref="Create"/> 工厂方法。
    /// </summary>
    public CloudDriveJob()
    {
    }

    /// <summary>
    /// 系统用户 ID。
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// 关联的 115 账号唯一标识。
    /// </summary>
    public string CloudAccountUid { get; private set; } = string.Empty;

    /// <summary>
    /// 任务类型。
    /// </summary>
    public CloudDriveJobType JobType { get; private set; }

    /// <summary>
    /// 任务状态。
    /// </summary>
    public CloudDriveJobStatus Status { get; private set; } = CloudDriveJobStatus.Pending;

    /// <summary>
    /// 关联资源标识。
    /// </summary>
    public string? ResourceId { get; private set; }

    /// <summary>
    /// 进度百分比，取值范围 0-100。
    /// </summary>
    public int Progress { get; private set; }

    /// <summary>
    /// 错误信息。
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// 业务载荷数据（JSON）。
    /// </summary>
    public string? PayloadJson { get; private set; }

    /// <summary>
    /// 后台任务恢复检查点数据（JSON）。
    /// </summary>
    public string? CheckpointData { get; private set; }

    /// <summary>
    /// 开始时间（UTC）。
    /// </summary>
    public DateTime? StartTime { get; private set; }

    /// <summary>
    /// 结束时间（UTC）。
    /// </summary>
    public DateTime? EndTime { get; private set; }

    /// <summary>
    /// 创建时间（UTC）。
    /// </summary>
    public DateTime CreationTime { get; private set; }

    /// <summary>
    /// 创建后台任务记录。
    /// </summary>
    /// <param name="userId">系统用户 ID。</param>
    /// <param name="cloudAccountUid">115 账号唯一标识。</param>
    /// <param name="jobType">任务类型。</param>
    /// <param name="resourceId">关联资源标识。</param>
    /// <param name="payloadJson">业务载荷数据（JSON）。</param>
    /// <returns>新创建的 <see cref="CloudDriveJob"/> 实例。</returns>
    public static CloudDriveJob Create(
        Guid userId,
        string cloudAccountUid,
        CloudDriveJobType jobType,
        string? resourceId = null,
        string? payloadJson = null)
    {
        return new CloudDriveJob
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CloudAccountUid = cloudAccountUid,
            JobType = jobType,
            Status = CloudDriveJobStatus.Pending,
            ResourceId = resourceId,
            Progress = 0,
            PayloadJson = payloadJson,
            CreationTime = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// 标记任务开始执行。
    /// </summary>
    public void Start()
    {
        StartTime = DateTime.UtcNow;
        EndTime = null;
        ErrorMessage = null;
        ChangeStatus(CloudDriveJobStatus.Running);
    }

    /// <summary>
    /// 更新任务进度。
    /// </summary>
    /// <param name="progress">进度百分比，取值范围 0-100。</param>
    public void UpdateProgress(int progress)
    {
        Progress = Math.Clamp(progress, 0, 100);
    }

    /// <summary>
    /// 更新恢复检查点。
    /// </summary>
    /// <param name="checkpointData">检查点数据（JSON）。</param>
    public void UpdateCheckpoint(string? checkpointData)
    {
        CheckpointData = checkpointData;
    }

    /// <summary>
    /// 标记任务成功完成。
    /// </summary>
    public void Complete()
    {
        Progress = 100;
        ErrorMessage = null;
        EndTime = DateTime.UtcNow;
        ChangeStatus(CloudDriveJobStatus.Completed);
    }

    /// <summary>
    /// 标记任务失败并记录错误信息。
    /// </summary>
    /// <param name="errorMessage">错误信息。</param>
    public void Fail(string errorMessage)
    {
        ErrorMessage = errorMessage;
        EndTime = DateTime.UtcNow;
        ChangeStatus(CloudDriveJobStatus.Failed);
    }

    /// <summary>
    /// 取消任务。
    /// </summary>
    public void Cancel()
    {
        EndTime = DateTime.UtcNow;
        ChangeStatus(CloudDriveJobStatus.Cancelled);
    }

    private void ChangeStatus(CloudDriveJobStatus newStatus)
    {
        if (Status == newStatus)
        {
            return;
        }

        CloudDriveJobStatus previousStatus = Status;
        Status = newStatus;

        RaiseDomainEvent(new CloudDriveJobStatusChanged(
            Id,
            UserId,
            CloudAccountUid,
            JobType,
            previousStatus,
            newStatus,
            DateTime.UtcNow));
    }
}
