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

    /// <summary>获取或设置 VIP 限制信息。</summary>
    public CloudDriveVipLimitDto? UserLimit { get; set; }

    /// <summary>获取或设置 VIP 等级限制列表。</summary>
    public IReadOnlyList<CloudDriveVipLimitDto> LimitList { get; set; } = [];

    /// <summary>获取或设置动态 VIP 等级对象。</summary>
    public IDictionary<string, object?> VipList { get; set; } = new Dictionary<string, object?>();

    /// <summary>获取或设置 VIP 卡详情。</summary>
    public CloudDriveYunCardDto? YunCard { get; set; }
}

/// <summary>115 VIP 限制项。</summary>
public sealed class CloudDriveVipLimitDto
{
    /// <summary>获取或设置功能标识。</summary>
    public string? Feature { get; set; }
    /// <summary>获取或设置等级。</summary>
    public string? Level { get; set; }
    /// <summary>获取或设置等级名称。</summary>
    public string? LevelName { get; set; }
    /// <summary>获取或设置限制数量。</summary>
    public string? LimitNumbers { get; set; }
}

/// <summary>115 VIP 云卡详情。</summary>
public sealed class CloudDriveYunCardDto
{
    /// <summary>获取或设置上游 00_balance 字段。</summary>
    public double Balance00 { get; set; }
    /// <summary>获取或设置永久权益数量。</summary>
    public double ForeverCount { get; set; }
    /// <summary>获取或设置卡数量。</summary>
    public double Num { get; set; }
    /// <summary>获取或设置空间数量。</summary>
    public double SpaceCount { get; set; }
    /// <summary>获取或设置取消兑换码数量。</summary>
    public string? SqcodeCancelCount { get; set; }
    /// <summary>获取或设置兑换码数量。</summary>
    public string? SqcodeCount { get; set; }
    /// <summary>获取或设置卡片地址。</summary>
    public string? Url { get; set; }
    /// <summary>获取或设置 VIP 数量。</summary>
    public double VipCount { get; set; }
}
