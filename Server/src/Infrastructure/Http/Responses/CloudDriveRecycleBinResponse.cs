using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

internal sealed class CloudDriveRecycleBinResponse
{
    [JsonConverter(typeof(CloudDriveRecycleItemListJsonConverter))]
    [JsonPropertyName("data")]
    public IReadOnlyList<CloudDriveRecycleItemResponse> Items { get; init; } = [];

    [JsonConverter(typeof(StringOrNumberToIntJsonConverter))]
    [JsonPropertyName("count")]
    public int Count { get; init; }

    [JsonConverter(typeof(NullableStringOrNumberToIntJsonConverter))]
    [JsonPropertyName("rb_pass")]
    public int? RecycleBinPasswordEnabled { get; init; }

    [JsonConverter(typeof(StringOrNumberToIntJsonConverter))]
    [JsonPropertyName("offset")]
    public int Offset { get; init; }

    [JsonConverter(typeof(StringOrNumberToIntJsonConverter))]
    [JsonPropertyName("page_size")]
    public int PageSize { get; init; }

    [JsonPropertyName("order")]
    public string? Order { get; init; }

    [JsonConverter(typeof(StringOrNumberToIntJsonConverter))]
    [JsonPropertyName("is_asc")]
    public int IsAscending { get; init; }
}
