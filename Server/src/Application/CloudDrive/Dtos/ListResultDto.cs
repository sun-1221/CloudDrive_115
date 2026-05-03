namespace Application.CloudDrive.Dtos;

/// <summary>
/// 列表结果包装。
/// </summary>
/// <typeparam name="T">列表项类型。</typeparam>
public class ListResultDto<T>
{
    /// <summary>
    /// 获取或设置列表项。
    /// </summary>
    public IReadOnlyList<T> Items { get; set; } = [];
}
