namespace Application.CloudDrive.Dtos;

/// <summary>
/// 115 用户聚合信息。
/// </summary>
public class CloudDriveUserInfoDto
{
    /// <summary>
    /// 获取或设置 115 账号 UID。
    /// </summary>
    public string CloudAccountUid { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置官方用户名。
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置小尺寸头像地址。
    /// </summary>
    public string? AvatarSmallUrl { get; set; }

    /// <summary>
    /// 获取或设置中尺寸头像地址。
    /// </summary>
    public string? AvatarMediumUrl { get; set; }

    /// <summary>
    /// 获取或设置大尺寸头像地址。
    /// </summary>
    public string? AvatarLargeUrl { get; set; }

    /// <summary>
    /// 获取或设置空间信息。
    /// </summary>
    public CloudDriveSpaceInfoDto? SpaceInfo { get; set; }

    /// <summary>
    /// 获取或设置 VIP 信息。
    /// </summary>
    public CloudDriveVipInfoDto? VipInfo { get; set; }
}
