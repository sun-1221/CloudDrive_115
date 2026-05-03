namespace Application.CloudDrive.Dtos;

/// <summary>
/// 115 字幕列表。
/// </summary>
public class CloudDriveSubtitleListDto
{
    /// <summary>
    /// 获取或设置自动加载字幕。
    /// </summary>
    public CloudDriveSubtitleDto? AutoloadSubtitle { get; set; }

    /// <summary>
    /// 获取或设置字幕条目。
    /// </summary>
    public IReadOnlyList<CloudDriveSubtitleDto> Items { get; set; } = [];
}
