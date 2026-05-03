using System.Text.Json;
using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

internal sealed class CloudDriveOfflineQuotaResponse
{
    [JsonPropertyName("package")]
    public JsonElement Package { get; init; }

    [JsonConverter(typeof(StringOrNumberToIntJsonConverter))]
    [JsonPropertyName("count")]
    public int Count { get; init; }

    [JsonConverter(typeof(StringOrNumberToIntJsonConverter))]
    [JsonPropertyName("surplus")]
    public int Surplus { get; init; }

    [JsonConverter(typeof(StringOrNumberToLongJsonConverter))]
    [JsonPropertyName("max_size")]
    public long MaxSize { get; init; }

    [JsonConverter(typeof(StringOrNumberToLongJsonConverter))]
    [JsonPropertyName("used")]
    public long Used { get; init; }
}
