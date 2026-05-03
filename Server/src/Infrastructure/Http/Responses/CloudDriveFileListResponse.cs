using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

/// <summary>
/// 115 文件列表响应模型。
/// </summary>
internal sealed class CloudDriveFileListResponse
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
    /// 获取或设置系统返回的总数。
    /// </summary>
    [JsonConverter(typeof(NullableStringOrNumberToIntJsonConverter))]
    [JsonPropertyName("sys_count")]
    public int? SystemCount { get; set; }

    /// <summary>
    /// 获取或设置文件数量。
    /// </summary>
    [JsonConverter(typeof(NullableStringOrNumberToIntJsonConverter))]
    [JsonPropertyName("file_count")]
    public int? FileCount { get; set; }

    /// <summary>
    /// 获取或设置文件夹数量。
    /// </summary>
    [JsonConverter(typeof(NullableStringOrNumberToIntJsonConverter))]
    [JsonPropertyName("folder_count")]
    public int? FolderCount { get; set; }

    /// <summary>
    /// 获取或设置分页大小。
    /// </summary>
    [JsonConverter(typeof(StringOrNumberToIntJsonConverter))]
    [JsonPropertyName("page_size")]
    public int PageSize { get; set; }

    /// <summary>
    /// 获取或设置目录标识。
    /// </summary>
    [JsonConverter(typeof(StringOrNumberToLongJsonConverter))]
    [JsonPropertyName("cid")]
    public long DirectoryId { get; set; }

    /// <summary>
    /// 获取或设置分页偏移量。
    /// </summary>
    [JsonConverter(typeof(StringOrNumberToIntJsonConverter))]
    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    /// <summary>
    /// 获取或设置分页限制。
    /// </summary>
    [JsonConverter(typeof(StringOrNumberToIntJsonConverter))]
    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    /// <summary>
    /// 获取或设置排序字段。
    /// </summary>
    [JsonPropertyName("order")]
    public string? Order { get; set; }

    /// <summary>
    /// 获取或设置是否升序排序。
    /// </summary>
    [JsonConverter(typeof(StringOrNumberToIntJsonConverter))]
    [JsonPropertyName("is_asc")]
    public int IsAscending { get; set; }

    /// <summary>
    /// 获取或设置面包屑路径。
    /// </summary>
    [JsonConverter(typeof(CloudDrive115BreadcrumbListJsonConverter))]
    [JsonPropertyName("path")]
    public IReadOnlyList<CloudDrive115BreadcrumbItem> Path { get; set; } = [];
}
