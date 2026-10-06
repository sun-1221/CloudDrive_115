namespace Application.CloudDrive.Dtos;

/// <summary>
/// 115 视频播放进度。
/// </summary>
public class CloudDriveVideoPlayProgressDto
{
    /// <summary>
    /// 获取或设置文件 ID。
    /// </summary>
    public string? FileId { get; set; }

    /// <summary>
    /// 获取或设置文件名。
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// 获取或设置官方 hash。
    /// </summary>
    public string? Hash { get; set; }

    /// <summary>
    /// 获取或设置提取码。
    /// </summary>
    public string PickCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置已播放秒数。
    /// </summary>
    public long? PlayedSeconds { get; set; }

    /// <summary>
    /// 获取或设置官方 add_time 时间戳。
    /// </summary>
    public long? AddTime { get; set; }

    /// <summary>
    /// 获取或设置播放进度 schema 状态。
    /// </summary>
    public string SchemaStatus { get; set; } = "available";
}
