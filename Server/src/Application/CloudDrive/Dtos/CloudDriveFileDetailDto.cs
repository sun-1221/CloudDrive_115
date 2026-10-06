namespace Application.CloudDrive.Dtos;

/// <summary>
/// 115 文件详情。
/// </summary>
public class CloudDriveFileDetailDto : CloudDriveFileDto
{
    /// <summary>
    /// 获取或设置详情来源。
    /// </summary>
    public string SchemaSource { get; set; } = "file_list_derived";

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
    /// 获取或设置路径树。
    /// </summary>
    public IReadOnlyList<CloudDrivePathNodeDto>? Paths { get; set; }
}
