namespace Application.CloudDrive.Dtos;

/// <summary>
/// 初始化上传输入。
/// </summary>
public class InitUploadInput : CloudDriveAccountScopedInput
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
}
