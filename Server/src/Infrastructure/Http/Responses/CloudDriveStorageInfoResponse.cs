using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

internal sealed class CloudDriveStorageInfoResponse
{
    [JsonPropertyName("all_total")]
    public string? AllTotal { get; init; }

    [JsonPropertyName("all_use")]
    public string? AllUse { get; init; }

    [JsonPropertyName("all_remain")]
    public string? AllRemain { get; init; }

    [JsonConverter(typeof(NullableStringOrNumberToLongJsonConverter))]
    [JsonPropertyName("all_total_size")]
    public long? AllTotalBytes { get; init; }

    [JsonConverter(typeof(NullableStringOrNumberToLongJsonConverter))]
    [JsonPropertyName("all_use_size")]
    public long? AllUseBytes { get; init; }

    [JsonConverter(typeof(NullableStringOrNumberToLongJsonConverter))]
    [JsonPropertyName("all_remain_size")]
    public long? AllRemainBytes { get; init; }

    [JsonPropertyName("size_format")]
    public string? SizeFormat { get; init; }
}
