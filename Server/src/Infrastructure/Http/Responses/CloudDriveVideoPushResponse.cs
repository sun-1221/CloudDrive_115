using System.Text.Json.Serialization;

namespace Infrastructure.Http.Responses;

internal sealed class CloudDriveVideoPushResponse
{
    [JsonPropertyName("data")]
    public string? Data { get; init; }

    [JsonPropertyName("request")]
    public string? Request { get; init; }
}
