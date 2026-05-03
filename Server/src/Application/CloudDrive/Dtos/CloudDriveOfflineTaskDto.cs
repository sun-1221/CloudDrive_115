namespace Application.CloudDrive.Dtos;

/// <summary>
/// 115 云下载任务。
/// </summary>
public class CloudDriveOfflineTaskDto
{
    /// <summary>
    /// 获取或设置任务哈希。
    /// </summary>
    public string InfoHash { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置任务名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置任务大小。
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// 获取或设置添加时间戳。
    /// </summary>
    public long? AddTime { get; set; }

    /// <summary>
    /// 获取或设置最后更新时间戳。
    /// </summary>
    public long? LastUpdate { get; set; }

    /// <summary>
    /// 获取或设置完成百分比。
    /// </summary>
    public decimal? PercentDone { get; set; }

    /// <summary>
    /// 获取或设置原始资源地址。
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// 获取或设置文件 ID。
    /// </summary>
    public string? FileId { get; set; }

    /// <summary>
    /// 获取或设置父目录 ID。
    /// </summary>
    public string? ParentId { get; set; }

    /// <summary>
    /// 获取或设置视频清晰度。
    /// </summary>
    public string? VideoDefinition { get; set; }

    /// <summary>
    /// 获取或设置播放时长。
    /// </summary>
    public long? PlayDuration { get; set; }

    /// <summary>
    /// 获取或设置归一化任务状态。
    /// </summary>
    public string? Status { get; set; }
}
