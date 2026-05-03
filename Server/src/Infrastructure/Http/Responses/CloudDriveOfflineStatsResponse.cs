using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

/// <summary>
/// 115 云下载统计响应模型。
/// </summary>
internal sealed class CloudDriveOfflineStatsResponse
{
    /// <summary>
    /// 获取或设置任务总数。
    /// </summary>
    [JsonConverter(typeof(StringOrNumberToIntJsonConverter))]
    [JsonPropertyName("total_count")]
    public int TotalCount { get; init; }

    /// <summary>
    /// 获取或设置失败任务数。
    /// </summary>
    [JsonConverter(typeof(StringOrNumberToIntJsonConverter))]
    [JsonPropertyName("failed_count")]
    public int FailedCount { get; init; }

    /// <summary>
    /// 获取或设置已完成任务数。
    /// </summary>
    [JsonConverter(typeof(StringOrNumberToIntJsonConverter))]
    [JsonPropertyName("finished_count")]
    public int FinishedCount { get; init; }

    /// <summary>
    /// 获取或设置下载中任务数。
    /// </summary>
    [JsonConverter(typeof(StringOrNumberToIntJsonConverter))]
    [JsonPropertyName("downloading_count")]
    public int DownloadingCount { get; init; }

    /// <summary>
    /// 获取或设置错误类型。
    /// </summary>
    [JsonPropertyName("errtype")]
    public string? ErrorType { get; init; }
}
