namespace Application.CloudDrive.Dtos;

/// <summary>
/// 115 文件夹信息。
/// </summary>
public class CloudDriveFolderDto
{
    /// <summary>
    /// 获取或设置文件夹 ID。
    /// </summary>
    public string FolderId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置文件夹名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置父目录 ID。
    /// </summary>
    public string? ParentId { get; set; }

    /// <summary>
    /// 获取或设置路径。
    /// </summary>
    public string? Path { get; set; }
}
