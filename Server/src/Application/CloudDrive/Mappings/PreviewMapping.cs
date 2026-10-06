using Application.CloudDrive.Dtos;

namespace Application.CloudDrive.Mappings;

/// <summary>
/// 预览相关手动映射。
/// </summary>
public static class PreviewMapping
{
    /// <summary>
    /// 将下载地址 DTO 映射为图片预览 DTO。
    /// </summary>
    /// <returns>返回当前操作的处理结果。</returns>
    public static CloudDriveImagePreviewDto ToDto(
        this CloudDriveDownloadDto download,
        string fileType)
    {
        return new CloudDriveImagePreviewDto
        {
            PreviewUrl = download.DownloadUrl,
            FileName = download.FileName,
            FileType = fileType,
            IsTemporary = true,
        };
    }

    /// <summary>
    /// 将下载地址 DTO 映射为文档预览 DTO。
    /// </summary>
    /// <returns>返回当前操作的处理结果。</returns>
    public static CloudDriveDocPreviewDto ToDto(
        this CloudDriveDownloadDto download,
        string fileType,
        DocRenderMode renderMode,
        string? thirdPartyPreviewUrl = null)
    {
        return new CloudDriveDocPreviewDto
        {
            PreviewUrl = download.DownloadUrl,
            FileName = download.FileName,
            FileType = fileType,
            RenderMode = renderMode,
            ThirdPartyPreviewUrl = thirdPartyPreviewUrl,
            IsTemporary = true,
        };
    }
}
