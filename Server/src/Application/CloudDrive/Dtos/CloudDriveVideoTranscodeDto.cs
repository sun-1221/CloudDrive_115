namespace Application.CloudDrive.Dtos;

/// <summary>
/// 115 视频转码状态。
/// </summary>
public class CloudDriveVideoTranscodeDto
{
    /// <summary>
    /// 获取或设置文件 ID。
    /// </summary>
    public string FileId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置转码状态。
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置关联后台任务 ID。
    /// </summary>
    public Guid? JobId { get; set; }
}
