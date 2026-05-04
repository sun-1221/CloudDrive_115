namespace Application.CloudDrive.Dtos;

/// <summary>
/// 115 文件搜索结果。
/// </summary>
public class CloudDriveSearchResultDto
{
    /// <summary>
    /// 获取或设置文件 ID。
    /// </summary>
    public string FileId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置网盘档案 ID。
    /// </summary>
    public string Aid { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置父目录 ID。
    /// </summary>
    public string ParentId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置文件名。
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置文件大小。
    /// </summary>
    public long? Size { get; set; }

    /// <summary>
    /// 获取或设置提取码。
    /// </summary>
    public string? PickCode { get; set; }

    /// <summary>
    /// 获取或设置官方 fc 原始分类码。
    /// </summary>
    public int? RawCategoryCode { get; set; }

    /// <summary>
    /// 获取或设置归一化分类。
    /// </summary>
    public string Category { get; set; } = "unknown";

    /// <summary>
    /// 获取或设置图标标识。
    /// </summary>
    public string? Ico { get; set; }

    /// <summary>
    /// 获取或设置 SHA1。
    /// </summary>
    public string? Sha1 { get; set; }

    /// <summary>
    /// 获取或设置目录显示名。
    /// </summary>
    public string? DirectoryName { get; set; }

    /// <summary>
    /// 获取或设置官方 ns 高亮片段原文。
    /// </summary>
    public string? HighlightNameRaw { get; set; }

    /// <summary>
    /// 获取或设置缩略图候选地址。
    /// </summary>
    public string? Thumb { get; set; }

    /// <summary>
    /// 获取或设置视频封面地址。
    /// </summary>
    public string? VideoImg { get; set; }

    /// <summary>
    /// 获取或设置是否为视频。
    /// </summary>
    public bool? IsVideo { get; set; }

    /// <summary>
    /// 获取或设置是否星标。
    /// </summary>
    public bool? IsStarred { get; set; }

    /// <summary>
    /// 获取或设置是否为私密文件。
    /// </summary>
    public bool? IsPrivate { get; set; }

    /// <summary>
    /// 获取或设置上传状态原始值。
    /// </summary>
    public int? UploadStatusRaw { get; set; }

    /// <summary>
    /// 获取或设置归一化上传状态。
    /// </summary>
    public string UploadStatus { get; set; } = "unknown";

    /// <summary>
    /// 获取或设置播放时长。
    /// </summary>
    public long? PlayDuration { get; set; }

    /// <summary>
    /// 获取或设置文件标签。
    /// </summary>
    public IReadOnlyList<CloudDriveFileLabelDto> Labels { get; set; } = [];

    /// <summary>
    /// 获取或设置条目 schema 状态。
    /// </summary>
    public string SchemaStatus { get; set; } = "available";
}
