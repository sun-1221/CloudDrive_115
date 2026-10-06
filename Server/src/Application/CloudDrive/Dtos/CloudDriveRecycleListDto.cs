namespace Application.CloudDrive.Dtos;

/// <summary>
/// 115 回收站列表顶层信息。
/// </summary>
public class CloudDriveRecycleListDto
{
    /// <summary>
    /// 获取或设置回收站条目列表。
    /// </summary>
    public IReadOnlyList<CloudDriveRecycleBinItemDto> Items { get; set; } = [];

    /// <summary>
    /// 获取或设置官方 count 字段。
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// 获取或设置官方 rb_pass 字段。
    /// </summary>
    public int RbPass { get; set; }

    /// <summary>
    /// 获取或设置分页偏移量。
    /// </summary>
    public int Offset { get; set; }

    /// <summary>
    /// 获取或设置官方 page_size 字段。
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 获取或设置排序字段。
    /// </summary>
    public string? Order { get; set; }

    /// <summary>
    /// 获取或设置官方 is_asc 字段。
    /// </summary>
    public int? IsAsc { get; set; }

    /// <summary>
    /// 获取或设置项目 schema 状态。
    /// </summary>
    public string ItemSchemaStatus { get; set; } = "not_collected";
}
