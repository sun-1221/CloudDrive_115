namespace Application.CloudDrive.Dtos;

/// <summary>
/// 115 云下载配额。
/// </summary>
public class CloudDriveOfflineQuotaDto
{
    /// <summary>
    /// 获取或设置云下载配额套餐。
    /// </summary>
    public object? Package { get; set; }

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

}
