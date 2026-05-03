namespace Application.CloudDrive.Dtos;

/// <summary>
/// 115 网盘空间信息。
/// </summary>
public class CloudDriveSpaceInfoDto
{
    /// <summary>
    /// 获取或设置总空间展示值。
    /// </summary>
    public string AllTotal { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置已用空间展示值。
    /// </summary>
    public string AllUse { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置剩余空间展示值。
    /// </summary>
    public string AllRemain { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置总空间字节数。
    /// </summary>
    public long AllTotalBytes { get; set; }

    /// <summary>
    /// 获取或设置已用空间字节数。
    /// </summary>
    public long AllUseBytes { get; set; }

    /// <summary>
    /// 获取或设置剩余空间字节数。
    /// </summary>
    public long AllRemainBytes { get; set; }
}
