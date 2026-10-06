using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

/// <summary>
/// 115 VIP 卡片响应模型。
/// </summary>
internal sealed class CloudDriveVipCardResponse
{
    /// <summary>
    /// 获取或设置数量。
    /// </summary>
    [JsonConverter(typeof(NullableStringOrNumberToIntJsonConverter))]
    [JsonPropertyName("num")]
    public int? Num { get; init; }

    /// <summary>
    /// 获取或设置资源地址。
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; init; }

    /// <summary>
    /// 获取或设置 VIP 卡数量。
    /// </summary>
    [JsonConverter(typeof(NullableStringOrNumberToIntJsonConverter))]
    [JsonPropertyName("vip_count")]
    public int? VipCount { get; init; }

    /// <summary>
    /// 获取或设置空间卡数量。
    /// </summary>
    [JsonConverter(typeof(NullableStringOrNumberToIntJsonConverter))]
    [JsonPropertyName("space_count")]
    public int? SpaceCount { get; init; }

    /// <summary>
    /// 获取或设置永久卡数量。
    /// </summary>
    [JsonConverter(typeof(NullableStringOrNumberToIntJsonConverter))]
    [JsonPropertyName("forever_count")]
    public int? ForeverCount { get; init; }

    /// <summary>
    /// 获取或设置安全码数量。
    /// </summary>
    [JsonConverter(typeof(NullableStringOrNumberToIntJsonConverter))]
    [JsonPropertyName("sqcode_count")]
    public int? SqCodeCount { get; init; }

    /// <summary>
    /// 获取或设置取消安全码数量。
    /// </summary>
    [JsonConverter(typeof(NullableStringOrNumberToIntJsonConverter))]
    [JsonPropertyName("sqcode_cancel_count")]
    public int? SqCodeCancelCount { get; init; }

    /// <summary>
    /// 获取或设置账户余额。
    /// </summary>
    [JsonConverter(typeof(NullableStringOrNumberToLongJsonConverter))]
    [JsonPropertyName("00_balance")]
    public long? Balance { get; init; }
}
