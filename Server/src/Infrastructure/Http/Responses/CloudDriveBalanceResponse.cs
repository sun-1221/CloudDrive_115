using System.Text.Json;
using System.Text.Json.Serialization;

namespace Infrastructure.Http.Responses;

internal sealed class CloudDriveBalanceResponse
{
    [JsonPropertyName("data")]
    public JsonElement Data { get; init; }
}
