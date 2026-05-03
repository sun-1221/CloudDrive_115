using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

internal sealed class CloudDriveSubtitleItemResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("file_name")]
    public string? FileName { get; init; }

    [JsonPropertyName("url")]
    public string? Url { get; init; }

    [JsonConverter(typeof(NullableStringOrNumberToLongJsonConverter))]
    [JsonPropertyName("size")]
    public long? Size { get; init; }

    [JsonPropertyName("language")]
    public string? Language { get; init; }
}
