namespace Application.CloudDrive.Dtos;

/// <summary>
/// 记忆视频播放进度输入。
/// </summary>
public class RememberVideoPlayProgressInput : CloudDriveAccountScopedInput
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
    /// 获取或设置当前播放进度秒数。
    /// </summary>
    public long Time { get; set; }

    /// <summary>
    /// 获取或设置是否播放完毕。
    /// </summary>
    public bool WatchEnd { get; set; }
}
