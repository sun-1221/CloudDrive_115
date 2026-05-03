using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

internal sealed class CloudDriveUploadCompleteResponse
{
    [JsonConverter(typeof(StringOrNumberToIntJsonConverter))]
    [JsonPropertyName("aid")]
    public int Aid { get; init; }

    [JsonPropertyName("cid")]
    public string? DirectoryId { get; init; }

    [JsonPropertyName("file_name")]
    public string? FileName { get; init; }

    [JsonConverter(typeof(NullableStringOrNumberToLongJsonConverter))]
    [JsonPropertyName("file_ptime")]
    public long? FilePTime { get; init; }

    [JsonConverter(typeof(NullableStringOrNumberToIntJsonConverter))]
    [JsonPropertyName("file_status")]
    public int? FileStatus { get; init; }

    [JsonPropertyName("file_id")]
    public string? FileId { get; init; }

    [JsonConverter(typeof(NullableStringOrNumberToLongJsonConverter))]
    [JsonPropertyName("file_size")]
    public long? FileSize { get; init; }

    [JsonPropertyName("pick_code")]
    public string? PickCode { get; init; }

    [JsonPropertyName("sha1")]
    public string? Sha1 { get; init; }

    [JsonConverter(typeof(NullableStringOrNumberToIntJsonConverter))]
    [JsonPropertyName("is_video")]
    public int? IsVideo { get; init; }

    [JsonPropertyName("user_id")]
    public string? UserId { get; init; }
}
