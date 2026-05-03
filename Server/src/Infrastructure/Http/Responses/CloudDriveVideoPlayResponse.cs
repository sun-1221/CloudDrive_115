using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

internal sealed class CloudDriveVideoPlayResponse
{
    [JsonPropertyName("pickcode")]
    public string? PickCode { get; init; }

    [JsonPropertyName("pick_code")]
    public string? PickCodeAlias { get; init; }

    [JsonPropertyName("file_name")]
    public string? FileName { get; init; }

    [JsonConverter(typeof(NullableStringOrNumberToLongJsonConverter))]
    [JsonPropertyName("file_size")]
    public long? FileSize { get; init; }

    [JsonConverter(typeof(NullableStringOrNumberToLongJsonConverter))]
    [JsonPropertyName("play_long")]
    public long? PlayDuration { get; init; }

    [JsonConverter(typeof(CloudDriveVideoUrlVariantListJsonConverter))]
    [JsonPropertyName("video_url")]
    public IReadOnlyList<CloudDriveVideoUrlVariantResponse> VideoUrls { get; init; } = [];

    [JsonConverter(typeof(CloudDriveVideoDefinitionListJsonConverter))]
    [JsonPropertyName("definition_list_new")]
    public IReadOnlyList<CloudDriveVideoDefinitionResponse> AvailableDefinitions { get; init; } = [];

    [JsonConverter(typeof(NullableStringOrNumberToIntJsonConverter))]
    [JsonPropertyName("definition")]
    public int? UserSelectedDefinition { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }
}
