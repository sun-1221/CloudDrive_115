namespace Application.CloudDrive.Dtos;

/// <summary>
/// 115 回收站项。
/// </summary>
public class CloudDriveRecycleBinItemDto : CloudDriveFileDto
{
    /// <summary>
    /// 获取或设置回收站项 ID，用于还原或彻底删除的 tid 参数。
    /// </summary>
    public string RecycleBinItemId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置还原状态，-1 表示还原中，0 表示正常。
    /// </summary>
    public int RestoreStatus { get; set; }

    /// <summary>
    /// 获取或设置删除时间。
    /// </summary>
    public DateTime DeletedTime { get; set; }

    /// <summary>
    /// 获取或设置原父目录 ID。
    /// </summary>
    public string? OriginalParentId { get; set; }

    /// <summary>
    /// 获取或设置原父目录名称。
    /// </summary>
    public string? OriginalParentName { get; set; }

    /// <summary>
    /// 获取或设置可安全推导的原路径。
    /// </summary>
    public string? OriginalPath { get; set; }
}
