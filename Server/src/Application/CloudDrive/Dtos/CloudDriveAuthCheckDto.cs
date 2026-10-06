namespace Application.CloudDrive.Dtos;

/// <summary>
/// 115 Cookie 登录态检查结果（含内部诊断字段）。
/// </summary>
public class CloudDriveAuthCheckDto
{
    /// <summary>
    /// 获取或设置 Cookie 会话是否已认证。
    /// </summary>
    public bool IsAuthenticated { get; set; }

    /// <summary>
    /// 获取或设置登录态。true 表示有效，false 表示失效，null 表示未知（网络异常等场景）。
    /// </summary>
    public bool? IsLoggedIn { get; set; }

    /// <summary>
    /// 获取或设置最后探测时间（UTC）。
    /// </summary>
    public DateTime LastCheckedAt { get; set; }

    /// <summary>
    /// 获取或设置冷却期截止时间（UTC）。
    /// </summary>
    public DateTime? CooldownUntil { get; set; }

    /// <summary>
    /// 获取或设置脱敏后的失败原因。
    /// </summary>
    public string? FailureReason { get; set; }
}
