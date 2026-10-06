namespace Application.CloudDrive.Dtos;

/// <summary>
/// 切换文件星标输入。
/// </summary>
public class ToggleStarInput
{
    /// <summary>
    /// 获取或设置文件或目录 ID。
    /// </summary>
    public string FileId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置是否星标。
    /// </summary>
    public bool Star { get; set; }
}
