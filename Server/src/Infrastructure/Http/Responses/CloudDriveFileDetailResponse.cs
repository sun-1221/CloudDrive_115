using System.Text.Json.Serialization;

namespace Infrastructure.Http.Responses;

/// <summary>
/// 115 文件详情响应模型。
/// </summary>
internal sealed class CloudDriveFileDetailResponse
{
    /// <summary>
    /// 获取或设置 File。
    /// </summary>
    [JsonPropertyName("file")]
    public CloudDrive115FileItem? File { get; init; }

    /// <summary>
    /// 获取或设置 115 响应的数据字段。
    /// </summary>
    [JsonPropertyName("data")]
    public CloudDrive115FileItem? Data { get; init; }
}
