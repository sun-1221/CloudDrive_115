namespace Application.CloudDrive.Dtos;

/// <summary>
/// 115 云下载任务统计。
/// </summary>
public class CloudDriveOfflineStatsDto
{
    /// <summary>
    /// 获取或设置任务总数。
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    /// 获取或设置进行中的任务数。
    /// </summary>
    public int Active { get; set; }

    /// <summary>
    /// 获取或设置已完成任务数。
    /// </summary>
    public int Completed { get; set; }

    /// <summary>
    /// 获取或设置失败任务数。
    /// </summary>
    public int Failed { get; set; }
}
