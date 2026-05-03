using System.Text.Json;
using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

[JsonConverter(typeof(CloudDriveRecycleItemJsonConverter))]
internal sealed class CloudDriveRecycleItemResponse
{
    public string Id { get; init; } = string.Empty;

    public string FileName { get; init; } = string.Empty;

    public int? Type { get; init; }

    public bool IsDirectory { get; init; }

    public long FileSize { get; init; }

    public long? DeletedTime { get; init; }

    public string? ThumbnailUrl { get; init; }

    public string? OriginalParentId { get; init; }

    public string? OriginalParentName { get; init; }

    public string? PickCode { get; init; }

    public int? RestoreStatus { get; init; }

    public JsonElement Raw { get; init; }
}
