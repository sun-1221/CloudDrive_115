using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

internal sealed class CloudDriveShareItemResponse
{
    [JsonPropertyName("share_code")]
    public string? ShareCode { get; init; }

    [JsonPropertyName("share_title")]
    public string? ShareTitle { get; init; }

    [JsonPropertyName("file_id")]
    public string? FileId { get; init; }

    [JsonPropertyName("file_name")]
    public string? FileName { get; init; }

    [JsonPropertyName("pick_code")]
    public string? PickCode { get; init; }

    [JsonConverter(typeof(NullableStringOrNumberToLongJsonConverter))]
    [JsonPropertyName("file_size")]
    public long? FileSize { get; init; }

    [JsonConverter(typeof(NullableStringOrNumberToLongJsonConverter))]
    [JsonPropertyName("create_time")]
    public long? CreateTime { get; init; }

    [JsonConverter(typeof(NullableStringOrNumberToLongJsonConverter))]
    [JsonPropertyName("expire_time")]
    public long? ExpireTime { get; init; }

    [JsonPropertyName("url")]
    public string? Url { get; init; }
}
