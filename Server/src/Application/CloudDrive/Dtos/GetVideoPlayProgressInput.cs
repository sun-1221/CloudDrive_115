namespace Application.CloudDrive.Dtos;

/// <summary>
/// 获取视频播放进度输入。
/// </summary>
public class GetVideoPlayProgressInput
{
    /// <summary>
    /// 获取或设置文件 ID。
    /// </summary>
    public string? FileId { get; set; }

    /// <summary>
    /// 获取或设置提取码。
    /// </summary>
    public string? PickCode { get; set; }
}
