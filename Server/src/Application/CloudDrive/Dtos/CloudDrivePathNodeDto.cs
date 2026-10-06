namespace Application.CloudDrive.Dtos;

/// <summary>
/// 115 文件路径节点。
/// </summary>
public class CloudDrivePathNodeDto
{
    /// <summary>
    /// 获取或设置空间分区 ID。
    /// </summary>
    public string Aid { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置目录 ID。
    /// </summary>
    public string FileId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置目录名称。
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置路径选中标记原始值。
    /// </summary>
    public string IsSelectedPath { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置父目录 ID。
    /// </summary>
    public string ParentId { get; set; } = string.Empty;
}
