using System.Text.Json.Serialization;

namespace Infrastructure.Http.Responses;

/// <summary>
/// 115 视频推送响应模型。
/// </summary>
internal sealed class CloudDriveVideoPushResponse
{
    /// <summary>
    /// 获取或设置 115 响应的数据字段。
    /// </summary>
    [JsonPropertyName("data")]
    public string? Data { get; init; }

    /// <summary>
    /// 获取或设置 115 响应回显的请求信息。
    /// </summary>
    [JsonPropertyName("request")]
    public string? Request { get; init; }
}
