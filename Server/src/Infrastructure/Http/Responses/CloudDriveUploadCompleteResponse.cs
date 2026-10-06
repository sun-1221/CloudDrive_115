using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

/// <summary>
/// 115 上传完成响应模型。
/// </summary>
internal sealed class CloudDriveUploadCompleteResponse
{
    /// <summary>
    /// 获取或设置 115 空间分区标识。
    /// </summary>
    [JsonConverter(typeof(StringOrNumberToIntJsonConverter))]
    [JsonPropertyName("aid")]
    public int Aid { get; init; }

    /// <summary>
    /// 获取或设置目录标识。
    /// </summary>
    [JsonPropertyName("cid")]
    public string? DirectoryId { get; init; }

    /// <summary>
    /// 获取或设置文件名称。
    /// </summary>
    [JsonPropertyName("file_name")]
    public string? FileName { get; init; }

    /// <summary>
    /// 获取或设置文件处理时间戳。
    /// </summary>
    [JsonConverter(typeof(NullableStringOrNumberToLongJsonConverter))]
    [JsonPropertyName("file_ptime")]
    public long? FilePTime { get; init; }

    /// <summary>
    /// 获取或设置文件状态。
    /// </summary>
    [JsonConverter(typeof(NullableStringOrNumberToIntJsonConverter))]
    [JsonPropertyName("file_status")]
    public int? FileStatus { get; init; }

    /// <summary>
    /// 获取或设置文件标识。
    /// </summary>
    [JsonPropertyName("file_id")]
    public string? FileId { get; init; }

    /// <summary>
    /// 获取或设置文件大小。
    /// </summary>
    [JsonConverter(typeof(NullableStringOrNumberToLongJsonConverter))]
    [JsonPropertyName("file_size")]
    public long? FileSize { get; init; }

    /// <summary>
    /// 获取或设置 115 文件提取码。
    /// </summary>
    [JsonPropertyName("pick_code")]
    public string? PickCode { get; init; }

    /// <summary>
    /// 获取或设置文件 SHA1。
    /// </summary>
    [JsonPropertyName("sha1")]
    public string? Sha1 { get; init; }

    /// <summary>
    /// 获取或设置当前条目是否为视频。
    /// </summary>
    [JsonConverter(typeof(NullableStringOrNumberToIntJsonConverter))]
    [JsonPropertyName("is_video")]
    public int? IsVideo { get; init; }

    /// <summary>
    /// 获取或设置用户标识。
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; init; }
}
