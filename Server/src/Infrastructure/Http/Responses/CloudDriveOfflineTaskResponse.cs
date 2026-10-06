using System.Text.Json;
using System.Text.Json.Serialization;

namespace Infrastructure.Http.Responses;

/// <summary>
/// 115 云下载任务响应模型。
/// </summary>
[JsonConverter(typeof(CloudDriveOfflineTaskJsonConverter))]
internal sealed class CloudDriveOfflineTaskResponse
{
    /// <summary>
    /// 获取或设置云下载任务哈希。
    /// </summary>
    public string InfoHash { get; init; } = string.Empty;

    /// <summary>
    /// 获取或设置名称。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 获取或设置文件大小。
    /// </summary>
    public long Size { get; init; }

    /// <summary>
    /// 获取或设置任务添加时间戳。
    /// </summary>
    public long? AddTime { get; init; }

    /// <summary>
    /// 获取或设置任务最后更新时间戳。
    /// </summary>
    public long? LastUpdate { get; init; }

    /// <summary>
    /// 获取或设置任务完成百分比。
    /// </summary>
    public decimal? PercentDone { get; init; }

    /// <summary>
    /// 获取或设置资源地址。
    /// </summary>
    public string? Url { get; init; }

    /// <summary>
    /// 获取或设置文件标识。
    /// </summary>
    public string? FileId { get; init; }

    /// <summary>
    /// 获取或设置父级目录标识。
    /// </summary>
    public string? ParentId { get; init; }

    /// <summary>
    /// 获取或设置视频清晰度标识。
    /// </summary>
    public string? VideoDefinition { get; init; }

    /// <summary>
    /// 获取或设置播放时长。
    /// </summary>
    public long? PlayDuration { get; init; }

    /// <summary>
    /// 获取或设置状态。
    /// </summary>
    public int? Status { get; init; }

    /// <summary>
    /// 获取或设置原始 JSON 对象。
    /// </summary>
    public JsonElement Raw { get; init; }
}
