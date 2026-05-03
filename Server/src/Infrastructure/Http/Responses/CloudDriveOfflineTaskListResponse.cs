using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

/// <summary>
/// 115 云下载任务列表响应模型。
/// </summary>
internal sealed class CloudDriveOfflineTaskListResponse
{
    /// <summary>
    /// 获取或设置当前页码。
    /// </summary>
    [JsonConverter(typeof(StringOrNumberToIntJsonConverter))]
    [JsonPropertyName("page")]
    public int Page { get; init; }

    /// <summary>
    /// 获取或设置总页数。
    /// </summary>
    [JsonConverter(typeof(StringOrNumberToIntJsonConverter))]
    [JsonPropertyName("page_count")]
    public int PageCount { get; init; }

    /// <summary>
    /// 获取或设置每页行数。
    /// </summary>
    [JsonConverter(typeof(StringOrNumberToIntJsonConverter))]
    [JsonPropertyName("page_row")]
    public int PageRow { get; init; }

    /// <summary>
    /// 获取或设置分页大小。
    /// </summary>
    [JsonConverter(typeof(StringOrNumberToIntJsonConverter))]
    [JsonPropertyName("page_size")]
    public int PageSize { get; init; }

    /// <summary>
    /// 获取或设置总数量。
    /// </summary>
    [JsonConverter(typeof(StringOrNumberToIntJsonConverter))]
    [JsonPropertyName("count")]
    public int Count { get; init; }

    /// <summary>
    /// 获取或设置云下载配额。
    /// </summary>
    [JsonConverter(typeof(StringOrNumberToIntJsonConverter))]
    [JsonPropertyName("quota")]
    public int Quota { get; init; }

    /// <summary>
    /// 获取或设置总数。
    /// </summary>
    [JsonConverter(typeof(StringOrNumberToIntJsonConverter))]
    [JsonPropertyName("total")]
    public int Total { get; init; }

    /// <summary>
    /// 获取或设置云下载任务列表。
    /// </summary>
    [JsonConverter(typeof(CloudDriveOfflineTaskListJsonConverter))]
    [JsonPropertyName("tasks")]
    public IReadOnlyList<CloudDriveOfflineTaskResponse> Tasks { get; init; } = [];

    /// <summary>
    /// 获取或设置真实状态标记。
    /// </summary>
    [JsonConverter(typeof(BoolOrNumberJsonConverter))]
    [JsonPropertyName("real_state")]
    public bool RealState { get; init; }

    /// <summary>
    /// 获取或设置错误类型。
    /// </summary>
    [JsonPropertyName("errtype")]
    public string? ErrorType { get; init; }
}
