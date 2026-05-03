using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

internal sealed class CloudDriveFileListResponse
{
    [JsonConverter(typeof(CloudDrive115FileItemListJsonConverter))]
    [JsonPropertyName("data")]
    public IReadOnlyList<CloudDrive115FileItem> Items { get; set; } = [];

    [JsonConverter(typeof(StringOrNumberToIntJsonConverter))]
    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonConverter(typeof(NullableStringOrNumberToIntJsonConverter))]
    [JsonPropertyName("sys_count")]
    public int? SystemCount { get; set; }

    [JsonConverter(typeof(NullableStringOrNumberToIntJsonConverter))]
    [JsonPropertyName("file_count")]
    public int? FileCount { get; set; }

    [JsonConverter(typeof(NullableStringOrNumberToIntJsonConverter))]
    [JsonPropertyName("folder_count")]
    public int? FolderCount { get; set; }

    [JsonConverter(typeof(StringOrNumberToIntJsonConverter))]
    [JsonPropertyName("page_size")]
    public int PageSize { get; set; }

    [JsonConverter(typeof(StringOrNumberToLongJsonConverter))]
    [JsonPropertyName("cid")]
    public long DirectoryId { get; set; }

    [JsonConverter(typeof(StringOrNumberToIntJsonConverter))]
    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    [JsonConverter(typeof(StringOrNumberToIntJsonConverter))]
    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("order")]
    public string? Order { get; set; }

    [JsonConverter(typeof(StringOrNumberToIntJsonConverter))]
    [JsonPropertyName("is_asc")]
    public int IsAscending { get; set; }

    [JsonConverter(typeof(CloudDrive115BreadcrumbListJsonConverter))]
    [JsonPropertyName("path")]
    public IReadOnlyList<CloudDrive115BreadcrumbItem> Path { get; set; } = [];
}
