namespace Application.CloudDrive.Dtos;

/// <summary>
/// 115 文件详情。
/// </summary>
public class CloudDriveFileDetailDto : CloudDriveFileDto
{
    /// <summary>
    /// 获取或设置创建时间。
    /// </summary>
    public DateTime CreatedTime { get; set; }

    /// <summary>
    /// 获取或设置精确字节大小。
    /// </summary>
    public long? SizeBytes { get; set; }

    /// <summary>
    /// 获取或设置文件数量。
    /// </summary>
    public int? FileCount { get; set; }

    /// <summary>
    /// 获取或设置子文件夹数量。
    /// </summary>
    public int? FolderCount { get; set; }

    /// <summary>
    /// 获取或设置父目录 ID。
    /// </summary>
    public string? ParentId { get; set; }

    /// <summary>
    /// 获取或设置路径树。
    /// </summary>
    public IReadOnlyList<CloudDrivePathNodeDto>? Paths { get; set; }
}
