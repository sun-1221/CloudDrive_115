using System.Text.Json;
using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

[JsonConverter(typeof(CloudDrive115FileItemJsonConverter))]
internal sealed class CloudDrive115FileItem
{
    public string FileId { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public long Size { get; init; }

    public int? FileCategory { get; init; }

    public bool IsDirectory { get; init; }

    public string? PickCode { get; init; }

    public string? ThumbnailUrl { get; init; }

    public string? OriginalUrl { get; init; }

    public string? VideoImageUrl { get; init; }

    public bool? IsVideo { get; init; }

    public string? VideoDefinition { get; init; }

    public long? PlayDuration { get; init; }

    public long? UploadTime { get; init; }

    public long? UpdateTime { get; init; }

    public bool? IsStarred { get; init; }

    public bool? IsPrivate { get; init; }

    public int? UploadStatus { get; init; }

    public string? Sha1 { get; init; }

    public string? Icon { get; init; }

    public string? FileClass { get; init; }

    public JsonElement Raw { get; init; }
}
