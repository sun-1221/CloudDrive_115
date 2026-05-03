using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

/// <summary>
/// 115 视频播放响应模型。
/// </summary>
internal sealed class CloudDriveVideoPlayResponse
{
    /// <summary>
    /// 获取或设置 115 文件提取码。
    /// </summary>
    [JsonPropertyName("pickcode")]
    public string? PickCode { get; init; }

    /// <summary>
    /// 获取或设置提取码别名。
    /// </summary>
    [JsonPropertyName("pick_code")]
    public string? PickCodeAlias { get; init; }

    /// <summary>
    /// 获取或设置文件名称。
    /// </summary>
    [JsonPropertyName("file_name")]
    public string? FileName { get; init; }

    /// <summary>
    /// 获取或设置文件大小。
    /// </summary>
    [JsonConverter(typeof(NullableStringOrNumberToLongJsonConverter))]
    [JsonPropertyName("file_size")]
    public long? FileSize { get; init; }

    /// <summary>
    /// 获取或设置播放时长。
    /// </summary>
    [JsonConverter(typeof(NullableStringOrNumberToLongJsonConverter))]
    [JsonPropertyName("play_long")]
    public long? PlayDuration { get; init; }

    /// <summary>
    /// 获取或设置视频播放地址列表。
    /// </summary>
    [JsonConverter(typeof(CloudDriveVideoUrlVariantListJsonConverter))]
    [JsonPropertyName("video_url")]
    public IReadOnlyList<CloudDriveVideoUrlVariantResponse> VideoUrls { get; init; } = [];

    /// <summary>
    /// 获取或设置可用清晰度列表。
    /// </summary>
    [JsonConverter(typeof(CloudDriveVideoDefinitionListJsonConverter))]
    [JsonPropertyName("definition_list_new")]
    public IReadOnlyList<CloudDriveVideoDefinitionResponse> AvailableDefinitions { get; init; } = [];

    /// <summary>
    /// 获取或设置用户选择的清晰度。
    /// </summary>
    [JsonConverter(typeof(NullableStringOrNumberToIntJsonConverter))]
    [JsonPropertyName("definition")]
    public int? UserSelectedDefinition { get; init; }

    /// <summary>
    /// 获取或设置官方记忆的用户上次选中清晰度。
    /// </summary>
    [JsonConverter(typeof(NullableStringOrNumberToIntJsonConverter))]
    [JsonPropertyName("user_def")]
    public int? UserDef { get; init; }

    /// <summary>
    /// 获取或设置记忆的视频旋转角度。
    /// </summary>
    [JsonConverter(typeof(NullableStringOrNumberToIntJsonConverter))]
    [JsonPropertyName("user_rotate")]
    public int? UserRotate { get; init; }

    /// <summary>
    /// 获取或设置记忆的视频翻转方向。
    /// </summary>
    [JsonConverter(typeof(NullableStringOrNumberToIntJsonConverter))]
    [JsonPropertyName("user_turn")]
    public int? UserTurn { get; init; }

    /// <summary>
    /// 获取或设置受限原因。
    /// </summary>
    [JsonPropertyName("restriction_reason")]
    public string? RestrictionReason { get; init; }

    /// <summary>
    /// 获取或设置状态。
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; init; }

    /// <summary>
    /// 获取或设置多音轨列表。
    /// </summary>
    [JsonPropertyName("multitrack_list")]
    public IReadOnlyList<CloudDriveVideoAudioTrackResponse> AudioTracks { get; init; } = [];
}
