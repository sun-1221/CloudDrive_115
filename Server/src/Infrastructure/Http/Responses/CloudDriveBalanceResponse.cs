using System.Text.Json;
using System.Text.Json.Serialization;

namespace Infrastructure.Http.Responses;

/// <summary>
/// 115 余额响应模型。
/// </summary>
internal sealed class CloudDriveBalanceResponse
{
    /// <summary>
    /// 获取或设置 115 响应的数据字段。
    /// </summary>
    [JsonPropertyName("data")]
    public JsonElement Data { get; init; }
}
