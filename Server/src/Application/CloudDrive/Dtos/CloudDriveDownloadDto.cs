namespace Application.CloudDrive.Dtos;

/// <summary>
/// 115 文件下载信息。
/// </summary>
public class CloudDriveDownloadDto
{
    /// <summary>
    /// 获取或设置临时下载地址或系统受控代理地址。
    /// </summary>
    public string DownloadUrl { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置文件名。
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置文件大小。
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// 获取或设置是否为临时资源。
    /// </summary>
    public bool IsTemporary { get; set; } = true;
}
