namespace Application.CloudDrive.Dtos;

/// <summary>
/// 删除文件输入。
/// </summary>
public class DeleteFilesInput : CloudDriveAccountScopedInput
{
    /// <summary>
    /// 获取或设置文件或目录 ID 列表。
    /// </summary>
    public List<string> FileIds { get; set; } = [];
}
