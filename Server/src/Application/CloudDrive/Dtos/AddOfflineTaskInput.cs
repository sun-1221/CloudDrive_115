namespace Application.CloudDrive.Dtos;

/// <summary>
/// 新增云下载任务输入。
/// </summary>
public class AddOfflineTaskInput : CloudDriveAccountScopedInput
{
    /// <summary>
    /// 获取或设置待下载 URL 列表。
    /// </summary>
    public List<string> Urls { get; set; } = [];

    /// <summary>
    /// 获取或设置保存路径。
    /// </summary>
    public string? SavePath { get; set; }
}
