namespace Application.CloudDrive.Dtos;

/// <summary>
/// 115 文档预览信息。
/// </summary>
public class CloudDriveDocPreviewDto
{
    /// <summary>
    /// 获取或设置临时下载、预览地址或系统受控代理地址。
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
    /// 获取或设置渲染方式。
    /// </summary>
    public DocRenderMode RenderMode { get; set; }

    /// <summary>
    /// 获取或设置第三方预览服务地址。
    /// </summary>
    public string? ThirdPartyPreviewUrl { get; set; }

    /// <summary>
    /// 获取或设置是否为临时资源。
    /// </summary>
    public bool IsTemporary { get; set; } = true;
}
