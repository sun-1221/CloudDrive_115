namespace Application.CloudDrive.Dtos;

/// <summary>
/// 115 文件搜索列表。
/// </summary>
public class CloudDriveSearchListDto
{
    /// <summary>
    /// 获取或设置搜索结果条目。
    /// </summary>
    public IReadOnlyList<CloudDriveSearchResultDto> Items { get; set; } = [];

    /// <summary>
    /// 获取或设置官方 count 字段。
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// 获取或设置官方 page_size 字段。
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 获取或设置官方 page_count 字段。
    /// </summary>
    public int PageCount { get; set; }

    /// <summary>
    /// 获取或设置分页偏移量。
    /// </summary>
    public int Offset { get; set; }

    /// <summary>
    /// 获取或设置官方 is_asc 字段。
    /// </summary>
    public int? IsAsc { get; set; }

    /// <summary>
    /// 获取或设置排序字段。
    /// </summary>
    public string? Order { get; set; }

    /// <summary>
    /// 获取或设置后缀筛选回显。
    /// </summary>
    public string? Suffix { get; set; }

    /// <summary>
    /// 获取或设置官方 fc_mix 字段。
    /// </summary>
    public int? FcMix { get; set; }

    /// <summary>
    /// 获取或设置官方 type 字段。
    /// </summary>
    public int? Type { get; set; }

    /// <summary>
    /// 获取或设置官方 state 字段。
    /// </summary>
    public bool? State { get; set; }

    /// <summary>
    /// 获取或设置官方 error 字段。
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// 获取或设置官方 errCode 字段。
    /// </summary>
    public int? ErrCode { get; set; }

    /// <summary>
    /// 获取或设置结果条目 schema 状态。
    /// </summary>
    public string ItemSchemaStatus { get; set; } = "available";
}
