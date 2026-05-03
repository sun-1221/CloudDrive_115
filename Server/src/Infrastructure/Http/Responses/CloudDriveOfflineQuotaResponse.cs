using System.Text.Json;
using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

/// <summary>
/// 115 云下载配额响应模型。
/// </summary>
internal sealed class CloudDriveOfflineQuotaResponse
{
    /// <summary>
    /// 获取或设置云下载配额套餐原始数据。
    /// </summary>
    [JsonPropertyName("package")]
    public JsonElement Package { get; init; }

    /// <summary>
    /// 获取或设置总数量。
    /// </summary>
    [JsonConverter(typeof(StringOrNumberToIntJsonConverter))]
    [JsonPropertyName("count")]
    public int Count { get; init; }

    /// <summary>
    /// 获取或设置剩余额度。
    /// </summary>
    [JsonConverter(typeof(StringOrNumberToIntJsonConverter))]
    [JsonPropertyName("surplus")]
    public int Surplus { get; init; }

    /// <summary>
    /// 获取或设置允许的最大任务大小。
    /// </summary>
    [JsonConverter(typeof(StringOrNumberToLongJsonConverter))]
    [JsonPropertyName("max_size")]
    public long MaxSize { get; init; }

    /// <summary>
    /// 获取或设置已使用额度。
    /// </summary>
    [JsonConverter(typeof(StringOrNumberToLongJsonConverter))]
    [JsonPropertyName("used")]
    public long Used { get; init; }
}
