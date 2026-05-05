namespace Application.CloudDrive.Dtos;

/// <summary>
/// 重命名文件输入。
/// </summary>
public class RenameFileInput
{
    /// <summary>
    /// 获取或设置文件或目录 ID。
    /// </summary>
    public string FileId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置新文件名。
    /// </summary>
    public string NewFileName { get; set; } = string.Empty;
}
