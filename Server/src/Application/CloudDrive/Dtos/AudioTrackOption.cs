namespace Application.CloudDrive.Dtos;

/// <summary>
/// 视频音轨选项。
/// </summary>
public class AudioTrackOption
{
    /// <summary>
    /// 获取或设置音轨索引。
    /// </summary>
    public int? TrackIndex { get; set; }

    /// <summary>
    /// 获取或设置音轨标题。
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// 获取或设置是否已选中。
    /// </summary>
    public bool? IsSelected { get; set; }
}
