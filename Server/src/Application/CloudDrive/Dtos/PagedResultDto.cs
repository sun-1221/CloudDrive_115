namespace Application.CloudDrive.Dtos;

/// <summary>
/// 分页结果包装。
/// </summary>
/// <typeparam name="T">列表项类型。</typeparam>
public class PagedResultDto<T>
{
    /// <summary>
    /// 获取或设置列表项。
    /// </summary>
    public IReadOnlyList<T> Items { get; set; } = [];

    /// <summary>
    /// 获取或设置总数量。
    /// </summary>
    public long TotalCount { get; set; }

    /// <summary>
    /// 获取或设置跳过数量。
    /// </summary>
    public int SkipCount { get; set; }

    /// <summary>
    /// 获取或设置最大返回数量。
    /// </summary>
    public int MaxResultCount { get; set; }

    /// <summary>
    /// 获取或设置面包屑路径（仅文件列表响应提供）。
    /// </summary>
    public IReadOnlyList<CloudDrivePathNodeDto>? Paths { get; set; }
}
