namespace Application.CloudDrive.Dtos;

/// <summary>
/// 115 云下载任务。
/// </summary>
public class CloudDriveOfflineTaskDto
{
    /// <summary>
    /// 获取或设置任务条目 schema 状态。
    /// </summary>
    public string SchemaStatus { get; set; } = "not_collected";
}
