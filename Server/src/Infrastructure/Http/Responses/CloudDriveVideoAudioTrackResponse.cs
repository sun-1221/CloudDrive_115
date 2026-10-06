using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

/// <summary>
/// 115 视频音轨响应模型。
/// </summary>
internal sealed class CloudDriveVideoAudioTrackResponse
{
    /// <summary>
    /// 获取或设置音轨索引。
    /// </summary>
    [JsonConverter(typeof(StringOrNumberToIntJsonConverter))]
    [JsonPropertyName("track")]
    public int TrackIndex { get; init; }

    /// <summary>
    /// 获取或设置音轨标题。
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    /// <summary>
    /// 获取或设置是否选中。
    /// </summary>
    [JsonConverter(typeof(NullableBoolOrNumberJsonConverter))]
    [JsonPropertyName("selected")]
    public bool? IsSelected { get; init; }
}
