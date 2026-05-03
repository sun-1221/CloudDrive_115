namespace Application.CloudDrive.Dtos;

/// <summary>
/// 115 文件路径节点。
/// </summary>
public class CloudDrivePathNodeDto
{
    /// <summary>
    /// 获取或设置目录 ID。
    /// </summary>
    public string FileId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置目录名称。
    /// </summary>
    public string FileName { get; set; } = string.Empty;
}
