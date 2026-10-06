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
    /// 获取或设置条目所属父目录 ID。
    /// </summary>
    public string? ParentId { get; set; }

    /// <summary>
    /// 获取或设置文件名。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置文件大小。
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// 获取或设置文件类型或扩展名（兼容字段，等于 FileClass ?? Ico）。
    /// </summary>
    public string FileType { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置原始文件分类码（115 fc 字段）。
    /// </summary>
    public int? RawCategoryCode { get; set; }

    /// <summary>
    /// 获取或设置文件分类名称（115 class 字段）。
    /// </summary>
    public string? FileClass { get; set; }

    /// <summary>
    /// 获取或设置文件图标标识（115 ico 字段）。
    /// </summary>
    public string? Ico { get; set; }

    /// <summary>
    /// 获取或设置是否为目录。
    /// </summary>
    public bool? IsDirectory { get; set; }

    /// <summary>
    /// 获取或设置修改时间。
    /// </summary>
    public DateTime ModifiedTime { get; set; }

    /// <summary>
    /// 获取或设置上传时间（115 te 字段）。
    /// </summary>
    public DateTime? UploadTime { get; set; }

    /// <summary>
    /// 获取或设置更新时间（115 t 字段）。
    /// </summary>
    public DateTime? UpdateTime { get; set; }

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
    /// 获取或设置是否为私密文件（115 hdf 字段）。
    /// </summary>
    public bool? IsPrivate { get; set; }

    /// <summary>
    /// 获取或设置上传状态原始值（115 sta 字段）。
    /// </summary>
    public int? UploadStatusRaw { get; set; }

    /// <summary>
    /// 获取或设置上传状态（值域未采集，默认 unknown）。
    /// </summary>
    public string UploadStatus { get; set; } = "unknown";

    /// <summary>
    /// 获取或设置缩略图地址。
    /// </summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>
    /// 获取或设置原图地址。
    /// </summary>
    public string? OriginalImageUrl { get; set; }

    /// <summary>
    /// 获取或设置视频封面地址（115 tp 字段）。
    /// </summary>
    public string? VideoImageUrl { get; set; }
}
