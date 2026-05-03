using System.Text.Json.Serialization;

namespace Infrastructure.Http.Responses;

internal sealed class CloudDriveSubtitleResponse
{
    [JsonPropertyName("autoload")]
    public IReadOnlyList<CloudDriveSubtitleItemResponse> AutoLoad { get; init; } = [];

    [JsonPropertyName("list")]
    public IReadOnlyList<CloudDriveSubtitleItemResponse> Items { get; init; } = [];
}
