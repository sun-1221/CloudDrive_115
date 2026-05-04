using System.Text.Json;
using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

/// <summary>
/// 115 文件搜索响应模型。
/// </summary>
internal sealed class CloudDriveFileSearchResponse
{
    /// <summary>
    /// 获取或设置条目列表。
    /// </summary>
    [JsonConverter(typeof(CloudDrive115FileItemListJsonConverter))]
    [JsonPropertyName("data")]
    public IReadOnlyList<CloudDrive115FileItem> Items { get; set; } = [];

    /// <summary>
    /// 获取或设置总数量。
    /// </summary>
    [JsonConverter(typeof(StringOrNumberToIntJsonConverter))]
    [JsonPropertyName("count")]
    public int Count { get; set; }

    /// <summary>
    /// 获取或设置分页大小。
    /// </summary>
    [JsonConverter(typeof(StringOrNumberToIntJsonConverter))]
    [JsonPropertyName("page_size")]
    public int PageSize { get; set; }

    /// <summary>
    /// 获取或设置总页数。
    /// </summary>
    [JsonConverter(typeof(NullableStringOrNumberToIntJsonConverter))]
    [JsonPropertyName("page_count")]
    public int? PageCount { get; set; }

    /// <summary>
    /// 获取或设置分页偏移量。
    /// </summary>
    [JsonConverter(typeof(StringOrNumberToIntJsonConverter))]
    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    /// <summary>
    /// 获取或设置是否升序排序。
    /// </summary>
    [JsonConverter(typeof(NullableStringOrNumberToIntJsonConverter))]
    [JsonPropertyName("is_asc")]
    public int? IsAscending { get; set; }

    /// <summary>
    /// 获取或设置排序字段。
    /// </summary>
    [JsonPropertyName("order")]
    public string? Order { get; set; }

    /// <summary>
    /// 获取或设置文件后缀筛选条件。
    /// </summary>
    [JsonPropertyName("suffix")]
    public string? Suffix { get; set; }

    /// <summary>
    /// 获取或设置官方 fc_mix 字段。
    /// </summary>
    [JsonConverter(typeof(NullableStringOrNumberToIntJsonConverter))]
    [JsonPropertyName("fc_mix")]
    public int? FcMix { get; set; }

    /// <summary>
    /// 获取或设置官方 type 字段。
    /// </summary>
    [JsonConverter(typeof(NullableStringOrNumberToIntJsonConverter))]
    [JsonPropertyName("type")]
    public int? Type { get; set; }

    /// <summary>
    /// 获取或设置官方 state 字段。
    /// </summary>
    [JsonPropertyName("state")]
    public JsonElement State { get; set; }

    /// <summary>
    /// 获取或设置官方 error 字段。
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>
    /// 获取或设置官方 errCode 字段。
    /// </summary>
    [JsonConverter(typeof(NullableStringOrNumberToIntJsonConverter))]
    [JsonPropertyName("errCode")]
    public int? ErrCode { get; set; }

    /// <summary>
    /// 获取或设置目录结果列表。
    /// </summary>
    [JsonConverter(typeof(CloudDrive115FileItemListJsonConverter))]
    [JsonPropertyName("folder")]
    public IReadOnlyList<CloudDrive115FileItem> Folders { get; set; } = [];
}
