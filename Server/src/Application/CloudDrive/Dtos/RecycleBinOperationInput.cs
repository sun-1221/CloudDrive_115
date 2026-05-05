namespace Application.CloudDrive.Dtos;

/// <summary>
/// 回收站批量操作输入。
/// </summary>
public class RecycleBinOperationInput
{
    /// <summary>
    /// 获取或设置回收站项 ID 列表。
    /// </summary>
    public List<string> RecycleBinItemIds { get; set; } = [];
}
