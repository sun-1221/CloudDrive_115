namespace Application.CloudDrive.Dtos;

/// <summary>
/// 新增云下载任务输入。
/// </summary>
public class AddOfflineTaskInput
{
    /// <summary>
    /// 获取或设置任务类型（Url / Bt）。
    /// </summary>
    public string TaskKind { get; set; } = "Url";

    /// <summary>
    /// 获取或设置待下载 URL 列表。
    /// </summary>
    public List<string> Urls { get; set; } = [];

    /// <summary>
    /// 获取或设置 BT 任务的 InfoHash。
    /// </summary>
    public string? InfoHash { get; set; }

    /// <summary>
    /// 获取或设置 BT 任务的选择性下载文件索引列表。
    /// </summary>
    public string? Wanted { get; set; }

    /// <summary>
    /// 获取或设置种子文件 SHA1。
    /// </summary>
    public string? TorrentSha1 { get; set; }

    /// <summary>
    /// 获取或设置续传提取码。
    /// </summary>
    public string? PickCode { get; set; }

    /// <summary>
    /// 获取或设置保存目录 ID。
    /// </summary>
    public string? SaveDirectoryId { get; set; }

    /// <summary>
    /// 获取或设置保存路径（兼容旧参数）。
    /// </summary>
    public string? SavePath { get; set; }
}
