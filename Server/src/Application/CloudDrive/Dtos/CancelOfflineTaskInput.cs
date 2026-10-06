namespace Application.CloudDrive.Dtos;

/// <summary>
/// 取消云下载任务输入。
/// </summary>
public class CancelOfflineTaskInput
{
    /// <summary>
    /// 获取或设置任务哈希。
    /// </summary>
    public string InfoHash { get; set; } = string.Empty;
}
