using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

internal sealed class CloudDrive115BreadcrumbItem
{
    [JsonPropertyName("cid")]
    public string DirectoryId { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonConverter(typeof(NullableStringOrNumberToLongJsonConverter))]
    [JsonPropertyName("aid")]
    public long? Aid { get; init; }

    [JsonPropertyName("pid")]
    public string? ParentId { get; init; }
}
