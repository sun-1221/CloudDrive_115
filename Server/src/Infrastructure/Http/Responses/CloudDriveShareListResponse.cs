using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

/// <summary>
/// 115 分享列表响应模型。
/// </summary>
internal sealed class CloudDriveShareListResponse
{
    /// <summary>
    /// 获取或设置总数量。
    /// </summary>
    [JsonConverter(typeof(StringOrNumberToIntJsonConverter))]
    [JsonPropertyName("count")]
    public int Count { get; init; }

    /// <summary>
    /// 获取或设置条目列表。
    /// </summary>
    [JsonPropertyName("list")]
    public IReadOnlyList<CloudDriveShareItemResponse> Items { get; init; } = [];

    /// <summary>
    /// 获取或设置自定义接收码状态。
    /// </summary>
    [JsonConverter(typeof(NullableStringOrNumberToIntJsonConverter))]
    [JsonPropertyName("define_receive_code")]
    public int? DefineReceiveCode { get; init; }

    /// <summary>
    /// 获取或设置绑定接收码状态。
    /// </summary>
    [JsonConverter(typeof(NullableStringOrNumberToIntJsonConverter))]
    [JsonPropertyName("bind_receive_code")]
    public int? BindReceiveCode { get; init; }
}
