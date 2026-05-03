namespace Application.CloudDrive.Dtos;

/// <summary>
/// 获取云下载任务列表输入。
/// </summary>
public class GetOfflineTaskListInput : CloudDriveAccountScopedInput
{
    /// <summary>
    /// 获取或设置页码。
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// 获取或设置任务状态过滤。
    /// </summary>
    public int Stat { get; set; }
}
