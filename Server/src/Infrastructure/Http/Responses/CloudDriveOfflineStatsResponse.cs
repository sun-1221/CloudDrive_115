using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

internal sealed class CloudDriveOfflineStatsResponse
{
    [JsonConverter(typeof(StringOrNumberToIntJsonConverter))]
    [JsonPropertyName("total_count")]
    public int TotalCount { get; init; }

    [JsonConverter(typeof(StringOrNumberToIntJsonConverter))]
    [JsonPropertyName("failed_count")]
    public int FailedCount { get; init; }

    [JsonConverter(typeof(StringOrNumberToIntJsonConverter))]
    [JsonPropertyName("finished_count")]
    public int FinishedCount { get; init; }

    [JsonConverter(typeof(StringOrNumberToIntJsonConverter))]
    [JsonPropertyName("downloading_count")]
    public int DownloadingCount { get; init; }

    [JsonPropertyName("errtype")]
    public string? ErrorType { get; init; }
}
