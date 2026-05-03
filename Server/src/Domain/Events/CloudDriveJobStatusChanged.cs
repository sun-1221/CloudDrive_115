using Domain.Enums;

namespace Domain.Events;

/// <summary>
/// 115 网盘后台任务状态变更领域事件。
/// </summary>
/// <param name="JobId">任务标识。</param>
/// <param name="UserId">系统用户 ID。</param>
/// <param name="CloudAccountUid">115 账号唯一标识。</param>
/// <param name="JobType">任务类型。</param>
/// <param name="PreviousStatus">变更前状态。</param>
/// <param name="NewStatus">变更后状态。</param>
/// <param name="ChangedAt">变更时间（UTC）。</param>
public sealed record CloudDriveJobStatusChanged(
    Guid JobId,
    Guid UserId,
    string CloudAccountUid,
    CloudDriveJobType JobType,
    CloudDriveJobStatus PreviousStatus,
    CloudDriveJobStatus NewStatus,
    DateTime ChangedAt) : DomainEvent;
