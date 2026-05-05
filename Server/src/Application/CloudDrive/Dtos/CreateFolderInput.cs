namespace Application.CloudDrive.Dtos;

/// <summary>
/// 创建文件夹输入。
/// </summary>
public class CreateFolderInput
{
    /// <summary>
    /// 获取或设置父目录 ID。
    /// </summary>
    public string ParentId { get; set; } = "0";

    /// <summary>
    /// 获取或设置文件夹名称。
    /// </summary>
    public string FolderName { get; set; } = string.Empty;
}
