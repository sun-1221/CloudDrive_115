namespace Application.CloudDrive.Dtos;

/// <summary>
/// 视频清晰度选项。
/// </summary>
public class VideoQualityOption
{
    /// <summary>
    /// 获取或设置清晰度标识。
    /// </summary>
    public int Definition { get; set; }

    /// <summary>
    /// 获取或设置新版清晰度标识。
    /// </summary>
    public int? DefinitionNew { get; set; }

    /// <summary>
    /// 获取或设置清晰度名称。
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// 获取或设置临时播放地址。
    /// </summary>
    public string PlayUrl { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置视频高度。
    /// </summary>
    public int? Height { get; set; }

    /// <summary>
    /// 获取或设置视频宽度。
    /// </summary>
    public int? Width { get; set; }
}
