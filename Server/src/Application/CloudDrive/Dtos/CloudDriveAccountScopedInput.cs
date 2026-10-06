namespace Application.CloudDrive.Dtos;

/// <summary>
/// 115 账号上下文输入。
/// </summary>
public class CloudDriveAccountScopedInput
{
    /// <summary>
    /// 获取或设置 115 账号 UID。
    /// </summary>
    public virtual string CloudAccountUid { get; set; } = string.Empty;
}
