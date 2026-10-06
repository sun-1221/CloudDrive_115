namespace Application.CloudDrive.Dtos;

/// <summary>
/// 115 用户聚合信息。
/// </summary>
public class CloudDriveUserInfoDto
{
    /// <summary>获取或设置基础资料 schema 状态。</summary>
    public string SchemaStatus { get; set; } = "not_collected";
    /// <summary>
    /// 获取或设置官方 user_id。
    /// </summary>
    public string Uid { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置官方昵称。
    /// </summary>
    public string Nickname { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置头像地址。
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// 获取或设置是否为 VIP。
    /// </summary>
    public bool IsVip { get; set; }

    /// <summary>
    /// 获取或设置已脱敏手机号。
    /// </summary>
    public string? MaskedPhone { get; set; }

    /// <summary>
    /// 获取或设置已脱敏邮箱。
    /// </summary>
    public string? MaskedEmail { get; set; }
}
