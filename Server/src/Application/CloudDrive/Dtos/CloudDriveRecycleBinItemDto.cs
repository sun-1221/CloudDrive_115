namespace Application.CloudDrive.Dtos;

/// <summary>
/// 115 回收站条目。
/// </summary>
public class CloudDriveRecycleBinItemDto
{
    /// <summary>获取或设置目录 ID。</summary>
    public string? Cid { get; set; }

    /// <summary>获取或设置删除时间原始值。</summary>
    public string? Dtime { get; set; }

    /// <summary>获取或设置文件名。</summary>
    public string? FileName { get; set; }

    /// <summary>获取或设置文件大小原始值。</summary>
    public string? FileSize { get; set; }

    /// <summary>获取或设置回收站条目标识。</summary>
    public string? Id { get; set; }

    /// <summary>获取或设置父目录名称。</summary>
    public string? ParentName { get; set; }

    /// <summary>获取或设置回收状态原始值。</summary>
    public string? Status { get; set; }

    /// <summary>获取或设置条目类型原始值。</summary>
    public string? Type { get; set; }

    /// <summary>
    /// 获取或设置条目 schema 状态。
    /// </summary>
    public string SchemaStatus { get; set; } = "not_collected";
}
