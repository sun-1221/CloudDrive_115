using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

internal sealed class CloudDriveOfflineTaskListResponse
{
    [JsonConverter(typeof(StringOrNumberToIntJsonConverter))]
    [JsonPropertyName("page")]
    public int Page { get; init; }

    [JsonConverter(typeof(StringOrNumberToIntJsonConverter))]
    [JsonPropertyName("page_count")]
    public int PageCount { get; init; }

    [JsonConverter(typeof(StringOrNumberToIntJsonConverter))]
    [JsonPropertyName("page_row")]
    public int PageRow { get; init; }

    [JsonConverter(typeof(StringOrNumberToIntJsonConverter))]
    [JsonPropertyName("page_size")]
    public int PageSize { get; init; }

    [JsonConverter(typeof(StringOrNumberToIntJsonConverter))]
    [JsonPropertyName("count")]
    public int Count { get; init; }

    [JsonConverter(typeof(StringOrNumberToIntJsonConverter))]
    [JsonPropertyName("quota")]
    public int Quota { get; init; }

    [JsonConverter(typeof(StringOrNumberToIntJsonConverter))]
    [JsonPropertyName("total")]
    public int Total { get; init; }

    [JsonConverter(typeof(CloudDriveOfflineTaskListJsonConverter))]
    [JsonPropertyName("tasks")]
    public IReadOnlyList<CloudDriveOfflineTaskResponse> Tasks { get; init; } = [];

    [JsonConverter(typeof(BoolOrNumberJsonConverter))]
    [JsonPropertyName("real_state")]
    public bool RealState { get; init; }

    [JsonPropertyName("errtype")]
    public string? ErrorType { get; init; }
}
