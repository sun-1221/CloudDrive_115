using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

internal sealed class CloudDriveVideoDefinitionResponse
{
    [JsonConverter(typeof(StringOrNumberToIntJsonConverter))]
    [JsonPropertyName("definition")]
    public int Definition { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }
}
