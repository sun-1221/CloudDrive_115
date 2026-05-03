using System.Text.Json;
using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

[JsonConverter(typeof(CloudDriveVideoUrlVariantJsonConverter))]
internal sealed class CloudDriveVideoUrlVariantResponse
{
    public int? Definition { get; init; }

    public string? DefinitionName { get; init; }

    public string Url { get; init; } = string.Empty;

    public long? Size { get; init; }

    public JsonElement Raw { get; init; }
}
