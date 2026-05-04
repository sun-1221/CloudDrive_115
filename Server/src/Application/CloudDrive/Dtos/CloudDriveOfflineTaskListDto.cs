namespace Application.CloudDrive.Dtos;

/// <summary>
/// 115 云下载任务列表。
/// </summary>
public class CloudDriveOfflineTaskListDto
{
    /// <summary>
    /// 获取或设置任务条目。
    /// </summary>
    public IReadOnlyList<CloudDriveOfflineTaskDto> Items { get; set; } = [];

    /// <summary>
    /// 获取或设置当前页码。
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// 获取或设置总页数。
    /// </summary>
    public int PageCount { get; set; }

    /// <summary>
    /// 获取或设置每页行数。
    /// </summary>
    public int PageRow { get; set; }

    /// <summary>
    /// 获取或设置分页大小。
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 获取或设置总数量。
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// 获取或设置配额数量。
    /// </summary>
    public int Quota { get; set; }

    /// <summary>
    /// 获取或设置总数。
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    /// 获取或设置官方真实状态标记。
    /// </summary>
    public bool? RealState { get; set; }

    /// <summary>
    /// 获取或设置错误类型。
    /// </summary>
    public string? ErrType { get; set; }

    /// <summary>
    /// 获取或设置任务条目 schema 状态。
    /// </summary>
    public string ItemSchemaStatus { get; set; } = "not_collected";
}
