namespace Application.CloudDrive.Dtos;

/// <summary>
/// 115 分享列表。
/// </summary>
public class CloudDriveShareListDto
{
    /// <summary>
    /// 获取或设置分享条目。
    /// </summary>
    public IReadOnlyList<CloudDriveShareItemDto> Items { get; set; } = [];

    /// <summary>
    /// 获取或设置分享总数。
    /// </summary>
    public int? Count { get; set; }

    /// <summary>
    /// 获取或设置定义接收码标记。
    /// </summary>
    public int? DefineReceiveCode { get; set; }

    /// <summary>
    /// 获取或设置绑定接收码标记。
    /// </summary>
    public int? BindReceiveCode { get; set; }

    /// <summary>
    /// 获取或设置官方 request 字段。
    /// </summary>
    public string? Request { get; set; }

    /// <summary>
    /// 获取或设置分享条目 schema 状态。
    /// </summary>
    public string ItemSchemaStatus { get; set; } = "not_collected";
}
