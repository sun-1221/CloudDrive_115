using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

internal sealed class CloudDriveVipCardResponse
{
    [JsonConverter(typeof(NullableStringOrNumberToIntJsonConverter))]
    [JsonPropertyName("num")]
    public int? Num { get; init; }

    [JsonPropertyName("url")]
    public string? Url { get; init; }

    [JsonConverter(typeof(NullableStringOrNumberToIntJsonConverter))]
    [JsonPropertyName("vip_count")]
    public int? VipCount { get; init; }

    [JsonConverter(typeof(NullableStringOrNumberToIntJsonConverter))]
    [JsonPropertyName("space_count")]
    public int? SpaceCount { get; init; }

    [JsonConverter(typeof(NullableStringOrNumberToIntJsonConverter))]
    [JsonPropertyName("forever_count")]
    public int? ForeverCount { get; init; }

    [JsonConverter(typeof(NullableStringOrNumberToIntJsonConverter))]
    [JsonPropertyName("sqcode_count")]
    public int? SqCodeCount { get; init; }

    [JsonConverter(typeof(NullableStringOrNumberToIntJsonConverter))]
    [JsonPropertyName("sqcode_cancel_count")]
    public int? SqCodeCancelCount { get; init; }

    [JsonConverter(typeof(NullableStringOrNumberToLongJsonConverter))]
    [JsonPropertyName("00_balance")]
    public long? Balance { get; init; }
}
