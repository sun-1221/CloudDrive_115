namespace Application.CloudDrive.Dtos;

/// <summary>
/// 115 回收站项占位。
/// </summary>
public class CloudDriveRecycleBinItemDto
{
    /// <summary>
    /// 获取或设置条目 schema 状态。
    /// </summary>
    public string SchemaStatus { get; set; } = "not_collected";
}
