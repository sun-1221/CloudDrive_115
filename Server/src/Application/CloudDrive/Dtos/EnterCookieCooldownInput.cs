namespace Application.CloudDrive.Dtos;

/// <summary>
/// Cookie 会话进入冷却期输入。
/// </summary>
public class EnterCookieCooldownInput : CloudDriveAccountScopedInput
{
    /// <summary>
    /// 获取或设置失效原因。
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置可选冷却秒数。
    /// </summary>
    public int? CooldownSeconds { get; set; }
}
