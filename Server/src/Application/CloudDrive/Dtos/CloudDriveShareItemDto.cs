namespace Application.CloudDrive.Dtos;

/// <summary>
/// 115 我的分享条目。
/// </summary>
public class CloudDriveShareItemDto
{
    /// <summary>
    /// 获取或设置分享码。
    /// </summary>
    public string ShareCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置分享标题。
    /// </summary>
    public string ShareTitle { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置文件标识。
    /// </summary>
    public string FileId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置文件名称。
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 115 文件提取码。
    /// </summary>
    public string? PickCode { get; set; }

    /// <summary>
    /// 获取或设置文件大小（字节）。
    /// </summary>
    public long? FileSize { get; set; }

    /// <summary>
    /// 获取或设置创建时间。
    /// </summary>
    public DateTime? CreateTime { get; set; }

    /// <summary>
    /// 获取或设置过期时间。
    /// </summary>
    public DateTime? ExpireTime { get; set; }

    /// <summary>
    /// 获取或设置资源地址。
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// 获取或设置分享条目 schema 状态。
    /// </summary>
    public string SchemaStatus { get; set; } = "available";
}
