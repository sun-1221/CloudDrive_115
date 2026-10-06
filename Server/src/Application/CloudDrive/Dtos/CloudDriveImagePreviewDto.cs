namespace Application.CloudDrive.Dtos;

/// <summary>
/// 115 图片预览信息。
/// </summary>
public class CloudDriveImagePreviewDto
{
    /// <summary>
    /// 获取或设置临时预览地址或系统受控代理地址。
    /// </summary>
    public string PreviewUrl { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置文件名。
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置文件类型。
    /// </summary>
    public string FileType { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置是否为临时资源。
    /// </summary>
    public bool IsTemporary { get; set; } = true;
}
