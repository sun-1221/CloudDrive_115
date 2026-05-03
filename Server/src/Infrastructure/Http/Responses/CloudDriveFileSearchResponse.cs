using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

internal sealed class CloudDriveFileSearchResponse
{
    [JsonConverter(typeof(CloudDrive115FileItemListJsonConverter))]
    [JsonPropertyName("data")]
    public IReadOnlyList<CloudDrive115FileItem> Items { get; set; } = [];

    [JsonConverter(typeof(StringOrNumberToIntJsonConverter))]
    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonConverter(typeof(StringOrNumberToIntJsonConverter))]
    [JsonPropertyName("page_size")]
    public int PageSize { get; set; }

    [JsonConverter(typeof(NullableStringOrNumberToIntJsonConverter))]
    [JsonPropertyName("page_count")]
    public int? PageCount { get; set; }

    [JsonConverter(typeof(StringOrNumberToIntJsonConverter))]
    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    [JsonConverter(typeof(NullableStringOrNumberToIntJsonConverter))]
    [JsonPropertyName("is_asc")]
    public int? IsAscending { get; set; }

    [JsonPropertyName("order")]
    public string? Order { get; set; }

    [JsonPropertyName("suffix")]
    public string? Suffix { get; set; }

    [JsonConverter(typeof(CloudDrive115FileItemListJsonConverter))]
    [JsonPropertyName("folder")]
    public IReadOnlyList<CloudDrive115FileItem> Folders { get; set; } = [];
}
