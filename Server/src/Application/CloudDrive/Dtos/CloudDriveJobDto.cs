using Domain.Enums;

namespace Application.CloudDrive.Dtos;

/// <summary>
/// 115 后台任务信息。
/// </summary>
public class CloudDriveJobDto
{
    /// <summary>
    /// 获取或设置任务 ID。
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 获取或设置任务类型。
    /// </summary>
    public CloudDriveJobType JobType { get; set; }

    /// <summary>
    /// 获取或设置任务状态。
    /// </summary>
    public CloudDriveJobStatus Status { get; set; }

    /// <summary>
    /// 获取或设置关联资源标识。
    /// </summary>
    public string? ResourceId { get; set; }

    /// <summary>
    /// 获取或设置进度百分比。
    /// </summary>
    public int Progress { get; set; }

    /// <summary>
    /// 获取或设置错误信息。
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 获取或设置开始时间。
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 获取或设置结束时间。
    /// </summary>
    public DateTime? EndTime { get; set; }
}
