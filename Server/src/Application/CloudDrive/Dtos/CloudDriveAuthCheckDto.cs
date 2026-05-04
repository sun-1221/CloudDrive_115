namespace Application.CloudDrive.Dtos;

/// <summary>
/// 115 Cookie 登录态公开检查结果。
/// </summary>
public class CloudDriveAuthCheckDto
{
    /// <summary>
    /// 获取或设置 Cookie 会话是否已认证。
    /// </summary>
    public bool IsAuthenticated { get; set; }

    /// <summary>
    /// 获取或设置脱敏后的失败原因。
    /// </summary>
    public string? FailureReason { get; set; }
}
