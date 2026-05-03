using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

internal sealed class CloudDriveShareListResponse
{
    [JsonConverter(typeof(StringOrNumberToIntJsonConverter))]
    [JsonPropertyName("count")]
    public int Count { get; init; }

    [JsonPropertyName("list")]
    public IReadOnlyList<CloudDriveShareItemResponse> Items { get; init; } = [];

    [JsonConverter(typeof(NullableStringOrNumberToIntJsonConverter))]
    [JsonPropertyName("define_receive_code")]
    public int? DefineReceiveCode { get; init; }

    [JsonConverter(typeof(NullableStringOrNumberToIntJsonConverter))]
    [JsonPropertyName("bind_receive_code")]
    public int? BindReceiveCode { get; init; }
}
