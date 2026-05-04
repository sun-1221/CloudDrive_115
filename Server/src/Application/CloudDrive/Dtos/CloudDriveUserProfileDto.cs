namespace Application.CloudDrive.Dtos;

/// <summary>
/// 115 用户中心聚合资料。
/// </summary>
public class CloudDriveUserProfileDto
{
    /// <summary>
    /// 获取或设置用户基础信息。
    /// </summary>
    public CloudDriveUserInfoDto? Info { get; set; }

    /// <summary>
    /// 获取或设置空间信息。
    /// </summary>
    public CloudDriveSpaceInfoDto? Storage { get; set; }

    /// <summary>
    /// 获取或设置 VIP 信息。
    /// </summary>
    public CloudDriveVipInfoDto? Vip { get; set; }

    /// <summary>
    /// 获取或设置余额信息。
    /// </summary>
    public CloudDriveBalanceDto? Balance { get; set; }
}
