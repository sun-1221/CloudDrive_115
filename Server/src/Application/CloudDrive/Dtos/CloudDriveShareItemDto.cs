namespace Application.CloudDrive.Dtos;

/// <summary>
/// 115 我的分享条目。
/// </summary>
public class CloudDriveShareItemDto
{
    /// <summary>
    /// 获取或设置分享码。
    /// </summary>
    public string? ShareCode { get; set; }

    /// <summary>
    /// 获取或设置分享标题。
    /// </summary>
    public string? ShareTitle { get; set; }

    /// <summary>
    /// 获取或设置文件 ID。
    /// </summary>
    public string? FileId { get; set; }

    /// <summary>
    /// 获取或设置文件名。
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// 获取或设置文件提取码。
    /// </summary>
    public string? PickCode { get; set; }

    /// <summary>
    /// 获取或设置文件大小。
    /// </summary>
    public long? FileSize { get; set; }

    /// <summary>
    /// 获取或设置创建时间戳。
    /// </summary>
    public long? CreateTime { get; set; }

    /// <summary>
    /// 获取或设置过期时间戳。
    /// </summary>
    public long? ExpireTime { get; set; }

    /// <summary>
    /// 获取或设置资源地址。
    /// </summary>
    public string? Url { get; set; }
}
