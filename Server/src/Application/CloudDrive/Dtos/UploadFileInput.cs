namespace Application.CloudDrive.Dtos;

/// <summary>
/// 上传文件输入。
/// </summary>
public class UploadFileInput
{
    /// <summary>
    /// 获取或设置上传目标目录 ID。
    /// </summary>
    public string DirectoryId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置文件名。
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置文件大小。
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// 获取或设置文件完整 SHA1。
    /// </summary>
    public string FileSha1 { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置文件前 128K SHA1。
    /// </summary>
    public string? PreSha1 { get; set; }

    /// <summary>
    /// 获取或设置续传提取码。
    /// </summary>
    public string? PickCode { get; set; }

    /// <summary>
    /// 获取或设置是否强制异步上传。
    /// </summary>
    public bool ForceAsync { get; set; }

    /// <summary>
    /// 获取或设置临时文件路径。
    /// </summary>
    public string? TempFilePath { get; set; }
}
