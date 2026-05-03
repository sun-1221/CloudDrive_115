namespace Application.CloudDrive.Dtos;

/// <summary>
/// 115 上传成功后的文件信息。
/// </summary>
public class CloudDriveUploadedFileDto
{
    /// <summary>
    /// 获取或设置文件 ID。
    /// </summary>
    public string FileId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置文件名。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置文件路径。
    /// </summary>
    public string? Path { get; set; }
}
