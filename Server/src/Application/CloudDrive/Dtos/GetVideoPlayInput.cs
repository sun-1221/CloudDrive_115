namespace Application.CloudDrive.Dtos;

/// <summary>
/// 获取视频播放地址输入。
/// </summary>
public class GetVideoPlayInput : CloudDriveAccountScopedInput
{
    /// <summary>
    /// 获取或设置文件 ID。
    /// </summary>
    public string? FileId { get; set; }

    /// <summary>
    /// 获取或设置提取码。
    /// </summary>
    public string? PickCode { get; set; }

    /// <summary>
    /// 获取或设置期望清晰度。
    /// </summary>
    public int? Definition { get; set; }
}
