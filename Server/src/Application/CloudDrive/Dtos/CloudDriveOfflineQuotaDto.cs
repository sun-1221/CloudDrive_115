namespace Application.CloudDrive.Dtos;

/// <summary>
/// 115 云下载配额与统计。
/// </summary>
public class CloudDriveOfflineQuotaDto
{
    /// <summary>
    /// 获取或设置云下载配额套餐。
    /// </summary>
    public Dictionary<string, object?>? Package { get; set; }

    /// <summary>
    /// 获取或设置配额数量。
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// 获取或设置剩余额度。
    /// </summary>
    public int Surplus { get; set; }

    /// <summary>
    /// 获取或设置允许的最大任务大小。
    /// </summary>
    public long MaxSize { get; set; }

    /// <summary>
    /// 获取或设置已使用额度。
    /// </summary>
    public long Used { get; set; }

    /// <summary>
    /// 获取或设置任务总数。
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    /// 获取或设置失败任务数。
    /// </summary>
    public int Failed { get; set; }

    /// <summary>
    /// 获取或设置已完成任务数。
    /// </summary>
    public int Completed { get; set; }

    /// <summary>
    /// 获取或设置下载中任务数。
    /// </summary>
    public int Active { get; set; }
}
