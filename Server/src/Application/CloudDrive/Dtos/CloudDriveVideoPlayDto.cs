namespace Application.CloudDrive.Dtos;

/// <summary>
/// 115 视频播放信息。
/// </summary>
public class CloudDriveVideoPlayDto
{
    /// <summary>
    /// 获取或设置播放状态。
    /// </summary>
    public VideoPlayStatus Status { get; set; } = VideoPlayStatus.Ready;

    /// <summary>
    /// 获取或设置自动提交转码后的后台任务 ID。
    /// </summary>
    public Guid? JobId { get; set; }

    /// <summary>
    /// 获取或设置可用清晰度列表。
    /// </summary>
    public IReadOnlyList<VideoQualityOption> QualityOptions { get; set; } = [];

    /// <summary>
    /// 获取或设置可用音轨列表。
    /// </summary>
    public IReadOnlyList<AudioTrackOption> AudioTracks { get; set; } = [];

    /// <summary>
    /// 获取或设置视频时长秒数。
    /// </summary>
    public int Duration { get; set; }

    /// <summary>
    /// 获取或设置官方记忆的用户上次选中清晰度。
    /// </summary>
    public int? UserDef { get; set; }

    /// <summary>
    /// 获取或设置本次用户请求的清晰度。
    /// </summary>
    public int? SelectedDefinition { get; set; }

    /// <summary>
    /// 获取或设置记忆的视频旋转角度。
    /// </summary>
    public int? UserRotate { get; set; }

    /// <summary>
    /// 获取或设置记忆的视频翻转方向。
    /// </summary>
    public int? UserTurn { get; set; }

    /// <summary>
    /// 获取或设置 4K/原画等受限原因。
    /// </summary>
    public string? RestrictionReason { get; set; }

    /// <summary>
    /// 获取或设置官方播放器元数据。
    /// </summary>
    public Dictionary<string, string>? PlaybackMetadata { get; set; }

    /// <summary>
    /// 获取或设置是否为临时资源。
    /// </summary>
    public bool IsTemporary { get; set; } = true;

    /// <summary>
    /// 获取或设置使用限制提示。
    /// </summary>
    public string? UsageRestrictionNotice { get; set; }
}
