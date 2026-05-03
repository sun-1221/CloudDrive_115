using System.Text.Json.Serialization;

namespace Infrastructure.Http.Responses;

internal sealed class CloudDriveFileDetailResponse
{
    [JsonPropertyName("file")]
    public CloudDrive115FileItem? File { get; init; }

    [JsonPropertyName("data")]
    public CloudDrive115FileItem? Data { get; init; }
}
