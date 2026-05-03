namespace Application.CloudDrive.Dtos;

/// <summary>
/// 115 字幕信息。
/// </summary>
public class CloudDriveSubtitleDto
{
    /// <summary>
    /// 获取或设置字幕 ID。
    /// </summary>
    public string SubtitleId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置字幕语言。
    /// </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置字幕标题。
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置字幕下载地址。
    /// </summary>
    public string SubtitleUrl { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置字幕类型。
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// 获取或设置官方 sha1。
    /// </summary>
    public string? Sha1 { get; set; }

    /// <summary>
    /// 获取或设置官方 file_id。
    /// </summary>
    public string? FileId { get; set; }

    /// <summary>
    /// 获取或设置字幕文件名。
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// 获取或设置关联视频提取码。
    /// </summary>
    public string? PickCode { get; set; }

    /// <summary>
    /// 获取或设置官方 caption_map_id。
    /// </summary>
    public string? CaptionMapId { get; set; }

    /// <summary>
    /// 获取或设置是否为 caption map 字幕。
    /// </summary>
    public bool? IsCaptionMap { get; set; }

    /// <summary>
    /// 获取或设置字幕同步时间。
    /// </summary>
    public int? SyncTime { get; set; }
}
