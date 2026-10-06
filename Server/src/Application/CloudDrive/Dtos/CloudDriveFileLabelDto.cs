namespace Application.CloudDrive.Dtos;

/// <summary>
/// 115 文件标签。
/// </summary>
public class CloudDriveFileLabelDto
{
    /// <summary>
    /// 获取或设置标签 ID。
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置标签名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置标签颜色。
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// 获取或设置标签排序。
    /// </summary>
    public int Sort { get; set; }
}
