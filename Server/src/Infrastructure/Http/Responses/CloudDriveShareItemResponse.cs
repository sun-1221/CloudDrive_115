using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

/// <summary>
/// 115 分享条目响应模型。
/// </summary>
internal sealed class CloudDriveShareItemResponse
{
    /// <summary>
    /// 获取或设置分享码。
    /// </summary>
    [JsonPropertyName("share_code")]
    public string? ShareCode { get; init; }

    /// <summary>
    /// 获取或设置分享标题。
    /// </summary>
    [JsonPropertyName("share_title")]
    public string? ShareTitle { get; init; }

    /// <summary>
    /// 获取或设置文件标识。
    /// </summary>
    [JsonPropertyName("file_id")]
    public string? FileId { get; init; }

    /// <summary>
    /// 获取或设置文件名称。
    /// </summary>
    [JsonPropertyName("file_name")]
    public string? FileName { get; init; }

    /// <summary>
    /// 获取或设置 115 文件提取码。
    /// </summary>
    [JsonPropertyName("pick_code")]
    public string? PickCode { get; init; }

    /// <summary>
    /// 获取或设置文件大小。
    /// </summary>
    [JsonConverter(typeof(NullableStringOrNumberToLongJsonConverter))]
    [JsonPropertyName("file_size")]
    public long? FileSize { get; init; }

    /// <summary>
    /// 获取或设置创建时间戳。
    /// </summary>
    [JsonConverter(typeof(NullableStringOrNumberToLongJsonConverter))]
    [JsonPropertyName("create_time")]
    public long? CreateTime { get; init; }

    /// <summary>
    /// 获取或设置过期时间戳。
    /// </summary>
    [JsonConverter(typeof(NullableStringOrNumberToLongJsonConverter))]
    [JsonPropertyName("expire_time")]
    public long? ExpireTime { get; init; }

    /// <summary>
    /// 获取或设置资源地址。
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; init; }
}
