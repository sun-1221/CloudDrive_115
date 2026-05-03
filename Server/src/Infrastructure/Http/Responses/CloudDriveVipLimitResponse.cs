using System.Text.Json;
using System.Text.Json.Serialization;

namespace Infrastructure.Http.Responses;

internal sealed class CloudDriveVipLimitResponse
{
    [JsonPropertyName("data")]
    public JsonElement Data { get; init; }
}
