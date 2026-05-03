using System.Text.Json;
using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

[JsonConverter(typeof(CloudDriveOfflineTaskJsonConverter))]
internal sealed class CloudDriveOfflineTaskResponse
{
    public string InfoHash { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public long Size { get; init; }

    public long? AddTime { get; init; }

    public long? LastUpdate { get; init; }

    public decimal? PercentDone { get; init; }

    public string? Url { get; init; }

    public string? FileId { get; init; }

    public string? ParentId { get; init; }

    public string? VideoDefinition { get; init; }

    public long? PlayDuration { get; init; }

    public int? Status { get; init; }

    public JsonElement Raw { get; init; }
}
