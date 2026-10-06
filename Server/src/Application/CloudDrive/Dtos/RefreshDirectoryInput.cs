namespace Application.CloudDrive.Dtos;

/// <summary>
/// 刷新目录缓存输入。
/// </summary>
public class RefreshDirectoryInput
{
    /// <summary>
    /// 获取或设置目录 ID，未指定时表示根目录。
    /// </summary>
    public string? DirectoryId { get; set; }
}
