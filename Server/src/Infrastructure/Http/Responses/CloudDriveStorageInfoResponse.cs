using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

/// <summary>
/// 115 存储空间响应模型。
/// </summary>
internal sealed class CloudDriveStorageInfoResponse
{
    /// <summary>
    /// 获取或设置总空间展示值。
    /// </summary>
    [JsonPropertyName("all_total")]
    public string? AllTotal { get; init; }

    /// <summary>
    /// 获取或设置已用空间展示值。
    /// </summary>
    [JsonPropertyName("all_use")]
    public string? AllUse { get; init; }

    /// <summary>
    /// 获取或设置剩余空间展示值。
    /// </summary>
    [JsonPropertyName("all_remain")]
    public string? AllRemain { get; init; }

    /// <summary>
    /// 获取或设置总空间字节数。
    /// </summary>
    [JsonConverter(typeof(NullableStringOrNumberToLongJsonConverter))]
    [JsonPropertyName("all_total_size")]
    public long? AllTotalBytes { get; init; }

    /// <summary>
    /// 获取或设置已用空间字节数。
    /// </summary>
    [JsonConverter(typeof(NullableStringOrNumberToLongJsonConverter))]
    [JsonPropertyName("all_use_size")]
    public long? AllUseBytes { get; init; }

    /// <summary>
    /// 获取或设置剩余空间字节数。
    /// </summary>
    [JsonConverter(typeof(NullableStringOrNumberToLongJsonConverter))]
    [JsonPropertyName("all_remain_size")]
    public long? AllRemainBytes { get; init; }

    /// <summary>
    /// 获取或设置空间大小格式。
    /// </summary>
    [JsonPropertyName("size_format")]
    public string? SizeFormat { get; init; }
}
