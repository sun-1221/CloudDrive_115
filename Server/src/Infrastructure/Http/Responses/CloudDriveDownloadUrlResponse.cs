using System.Text.Json;
using System.Text.Json.Serialization;

namespace Infrastructure.Http.Responses;

/// <summary>
/// 115 下载地址响应模型。
/// </summary>
internal sealed class CloudDriveDownloadUrlResponse
{
    /// <summary>
    /// 获取或设置资源地址。
    /// </summary>
    [JsonPropertyName("file_url_302")]
    public string? Url { get; init; }

    /// <summary>
    /// 获取或设置 115 文件提取码。
    /// </summary>
    [JsonPropertyName("pickcode")]
    public string? PickCode { get; init; }

    [JsonPropertyName("file_id")]
    public string? FileId { get; init; }

    [JsonPropertyName("file_size")]
    public long FileSize { get; init; }

    /// <summary>
    /// 获取或设置文件名称。
    /// </summary>
    [JsonPropertyName("file_name")]
    public string? FileName { get; init; }

    [JsonPropertyName("msg_code")]
    public int? MessageCode { get; init; }

    /// <summary>
    /// 获取或设置 115 响应的数据字段。
    /// </summary>
    [JsonPropertyName("data")]
    public JsonElement? Data { get; init; }
}
