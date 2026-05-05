namespace Application.CloudDrive.Dtos;

/// <summary>
/// 清空云下载任务输入。
/// </summary>
public class ClearOfflineTasksInput
{
    /// <summary>
    /// 获取或设置任务状态过滤。
    /// </summary>
    public int? Stat { get; set; }
}
