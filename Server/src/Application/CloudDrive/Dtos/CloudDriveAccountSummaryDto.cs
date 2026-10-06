using Domain.Enums;

namespace Application.CloudDrive.Dtos;

/// <summary>
/// 115 账号摘要。
/// </summary>
public class CloudDriveAccountSummaryDto
{
    /// <summary>
    /// 获取或设置 115 账号 UID。
    /// </summary>
    public string CloudAccountUid { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置会话状态。
    /// </summary>
    public CookieSessionStatus Status { get; set; }

    /// <summary>
    /// 获取或设置创建时间。
    /// </summary>
    public DateTime CreationTime { get; set; }

    /// <summary>
    /// 获取或设置最后修改时间。
    /// </summary>
    public DateTime? LastModificationTime { get; set; }

    /// <summary>
    /// 获取或设置冷却期截止时间。
    /// </summary>
    public DateTime? CooldownUntil { get; set; }
}
