namespace Application.CloudDrive.Dtos;

/// <summary>
/// 115 网盘空间信息。
/// </summary>
public class CloudDriveSpaceInfoDto
{
    /// <summary>
    /// 获取或设置空间详情 schema 状态。
    /// </summary>
    public string SchemaStatus { get; set; } = "not_collected";

    /// <summary>获取或设置总空间。</summary>
    public CloudDriveSpaceUsageDto? AllTotal { get; set; }

    /// <summary>获取或设置已使用空间。</summary>
    public CloudDriveSpaceUsageDto? AllUse { get; set; }

    /// <summary>获取或设置剩余空间。</summary>
    public CloudDriveSpaceUsageDto? AllRemain { get; set; }

    /// <summary>获取或设置分类数量。</summary>
    public IDictionary<string, long> TypeCounts { get; set; } = new Dictionary<string, long>();

    /// <summary>获取或设置分类空间汇总。</summary>
    public IDictionary<string, CloudDriveTypeSummaryDto> TypeSummaries { get; set; } = new Dictionary<string, CloudDriveTypeSummaryDto>();
}

/// <summary>115 空间用量。</summary>
public sealed class CloudDriveSpaceUsageDto
{
    /// <summary>获取或设置空间大小。</summary>
    public long Size { get; set; }

    /// <summary>获取或设置格式化空间大小。</summary>
    public string? SizeFormat { get; set; }
}

/// <summary>115 分类空间汇总。</summary>
public sealed class CloudDriveTypeSummaryDto
{
    /// <summary>获取或设置分类数量。</summary>
    public long Count { get; set; }

    /// <summary>获取或设置上游拼写保持不变的比例字段。</summary>
    public double Pecent { get; set; }

    /// <summary>获取或设置分类空间大小。</summary>
    public long Size { get; set; }

    /// <summary>获取或设置格式化分类空间大小。</summary>
    public string? SizeFormat { get; set; }
}
