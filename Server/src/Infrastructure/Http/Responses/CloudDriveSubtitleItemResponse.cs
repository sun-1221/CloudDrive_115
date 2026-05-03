using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

/// <summary>
/// 115 字幕条目响应模型。
/// </summary>
internal sealed class CloudDriveSubtitleItemResponse
{
    /// <summary>
    /// 获取或设置标识。
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    /// <summary>
    /// 获取或设置标题。
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    /// <summary>
    /// 获取或设置文件名称。
    /// </summary>
    [JsonPropertyName("file_name")]
    public string? FileName { get; init; }

    /// <summary>
    /// 获取或设置资源地址。
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; init; }

    /// <summary>
    /// 获取或设置文件大小。
    /// </summary>
    [JsonConverter(typeof(NullableStringOrNumberToLongJsonConverter))]
    [JsonPropertyName("size")]
    public long? Size { get; init; }

    /// <summary>
    /// 获取或设置语言。
    /// </summary>
    [JsonPropertyName("language")]
    public string? Language { get; init; }
}
