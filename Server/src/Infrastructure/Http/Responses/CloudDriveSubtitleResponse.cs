using System.Text.Json.Serialization;

namespace Infrastructure.Http.Responses;

/// <summary>
/// 115 字幕列表响应模型。
/// </summary>
internal sealed class CloudDriveSubtitleResponse
{
    /// <summary>
    /// 获取或设置自动加载字幕列表。
    /// </summary>
    [JsonPropertyName("autoload")]
    public IReadOnlyList<CloudDriveSubtitleItemResponse> AutoLoad { get; init; } = [];

    /// <summary>
    /// 获取或设置条目列表。
    /// </summary>
    [JsonPropertyName("list")]
    public IReadOnlyList<CloudDriveSubtitleItemResponse> Items { get; init; } = [];
}
