using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

/// <summary>
/// 115 回收站列表响应模型。
/// </summary>
internal sealed class CloudDriveRecycleBinResponse
{
    /// <summary>
    /// 获取或设置条目列表。
    /// </summary>
    [JsonConverter(typeof(CloudDriveRecycleItemListJsonConverter))]
    [JsonPropertyName("data")]
    public IReadOnlyList<CloudDriveRecycleItemResponse> Items { get; init; } = [];

    /// <summary>
    /// 获取或设置总数量。
    /// </summary>
    [JsonConverter(typeof(StringOrNumberToIntJsonConverter))]
    [JsonPropertyName("count")]
    public int Count { get; init; }

    /// <summary>
    /// 获取或设置回收站密码启用状态。
    /// </summary>
    [JsonConverter(typeof(NullableStringOrNumberToIntJsonConverter))]
    [JsonPropertyName("rb_pass")]
    public int? RecycleBinPasswordEnabled { get; init; }

    /// <summary>
    /// 获取或设置分页偏移量。
    /// </summary>
    [JsonConverter(typeof(StringOrNumberToIntJsonConverter))]
    [JsonPropertyName("offset")]
    public int Offset { get; init; }

    /// <summary>
    /// 获取或设置分页大小。
    /// </summary>
    [JsonConverter(typeof(StringOrNumberToIntJsonConverter))]
    [JsonPropertyName("page_size")]
    public int PageSize { get; init; }

    /// <summary>
    /// 获取或设置排序字段。
    /// </summary>
    [JsonPropertyName("order")]
    public string? Order { get; init; }

    /// <summary>
    /// 获取或设置是否升序排序。
    /// </summary>
    [JsonConverter(typeof(StringOrNumberToIntJsonConverter))]
    [JsonPropertyName("is_asc")]
    public int IsAscending { get; init; }
}
