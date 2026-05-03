namespace Application.CloudDrive.Dtos;

/// <summary>
/// 移动文件输入。
/// </summary>
public class MoveFilesInput : CloudDriveAccountScopedInput
{
    /// <summary>
    /// 获取或设置文件或目录 ID 列表。
    /// </summary>
    public List<string> FileIds { get; set; } = [];

    /// <summary>
    /// 获取或设置目标目录 ID。
    /// </summary>
    public string TargetDirectoryId { get; set; } = string.Empty;
}
