namespace Application.CloudDrive.Dtos;

/// <summary>
/// 115 VIP 信息。
/// </summary>
public class CloudDriveVipInfoDto
{
    /// <summary>
    /// 获取或设置 VIP 等级名称。
    /// </summary>
    public string? LevelName { get; set; }

    /// <summary>
    /// 获取或设置 VIP 过期时间。
    /// </summary>
    public DateTime? Expire { get; set; }
}
