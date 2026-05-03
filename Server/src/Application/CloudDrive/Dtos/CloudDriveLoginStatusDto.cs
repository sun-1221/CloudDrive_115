namespace Application.CloudDrive.Dtos;

/// <summary>
/// 115 登录态探测结果。
/// </summary>
public class CloudDriveLoginStatusDto
{
    /// <summary>
    /// 获取或设置 115 账号 UID。
    /// </summary>
    public string CloudAccountUid { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置登录态。true 表示有效，false 表示失效，null 表示未知。
    /// </summary>
    public bool? IsLoggedIn { get; set; }

    /// <summary>
    /// 获取或设置最后探测时间。
    /// </summary>
    public DateTime LastCheckedAt { get; set; }

    /// <summary>
    /// 获取或设置冷却期截止时间。
    /// </summary>
    public DateTime? CooldownUntil { get; set; }

    /// <summary>
    /// 获取或设置脱敏后的失败原因。
    /// </summary>
    public string? FailureReason { get; set; }
}
