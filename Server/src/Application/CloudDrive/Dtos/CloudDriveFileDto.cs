namespace Application.CloudDrive.Dtos;

/// <summary>
/// 115 文件列表项。
/// </summary>
public class CloudDriveFileDto
{
    /// <summary>
    /// 获取或设置文件或目录 ID。
    /// </summary>
    public string FileId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置文件名。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置文件大小。
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// 获取或设置文件类型或扩展名。
    /// </summary>
    public string FileType { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置是否为目录。
    /// </summary>
    public bool IsDirectory { get; set; }

    /// <summary>
    /// 获取或设置修改时间。
    /// </summary>
    public DateTime ModifiedTime { get; set; }

    /// <summary>
    /// 获取或设置 115 提取码。
    /// </summary>
    public string? PickCode { get; set; }

    /// <summary>
    /// 获取或设置 SHA1 哈希。
    /// </summary>
    public string? Sha1 { get; set; }

    /// <summary>
    /// 获取或设置是否星标。
    /// </summary>
    public bool IsStar { get; set; }

    /// <summary>
    /// 获取或设置文件标签。
    /// </summary>
    public IReadOnlyList<CloudDriveFileLabelDto>? Labels { get; set; }

    /// <summary>
    /// 获取或设置是否为视频。
    /// </summary>
    public bool IsVideo { get; set; }

    /// <summary>
    /// 获取或设置音视频时长秒数。
    /// </summary>
    public long? PlayLong { get; set; }

    /// <summary>
    /// 获取或设置视频清晰度。
    /// </summary>
    public int? VideoDefinition { get; set; }

    /// <summary>
    /// 获取或设置缩略图地址。
    /// </summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>
    /// 获取或设置原图地址。
    /// </summary>
    public string? OriginalImageUrl { get; set; }
}
