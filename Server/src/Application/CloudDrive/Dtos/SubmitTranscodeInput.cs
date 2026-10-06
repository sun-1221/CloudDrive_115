namespace Application.CloudDrive.Dtos;

/// <summary>
/// 提交视频转码输入。
/// </summary>
public class SubmitTranscodeInput
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
    /// 获取或设置转码方式。
    /// </summary>
    public string Op { get; set; } = "vip_push";
}
