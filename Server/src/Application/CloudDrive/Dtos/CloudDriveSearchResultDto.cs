namespace Application.CloudDrive.Dtos;

/// <summary>
/// 115 文件搜索结果。
/// </summary>
public class CloudDriveSearchResultDto : CloudDriveFileDto
{
    /// <summary>
    /// 获取或设置父目录 ID。
    /// </summary>
    public string ParentId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置系统可安全推导的路径。
    /// </summary>
    public string? Path { get; set; }
}
