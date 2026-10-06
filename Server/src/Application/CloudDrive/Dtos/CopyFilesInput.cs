namespace Application.CloudDrive.Dtos;

/// <summary>
/// 复制文件输入。
/// </summary>
public class CopyFilesInput
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
