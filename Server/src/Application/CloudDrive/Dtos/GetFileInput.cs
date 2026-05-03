namespace Application.CloudDrive.Dtos;

/// <summary>
/// 获取单个文件输入。
/// </summary>
public class GetFileInput : CloudDriveAccountScopedInput
{
    /// <summary>
    /// 获取或设置文件或目录 ID。
    /// </summary>
    public string? FileId { get; set; }

    /// <summary>
    /// 获取或设置文件路径。
    /// </summary>
    public string? Path { get; set; }
}
