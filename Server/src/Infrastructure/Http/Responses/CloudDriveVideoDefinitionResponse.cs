using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

/// <summary>
/// 115 视频清晰度响应模型。
/// </summary>
internal sealed class CloudDriveVideoDefinitionResponse
{
    /// <summary>
    /// 获取或设置视频清晰度值。
    /// </summary>
    [JsonConverter(typeof(StringOrNumberToIntJsonConverter))]
    [JsonPropertyName("definition")]
    public int Definition { get; init; }

    /// <summary>
    /// 获取或设置新版视频清晰度值。
    /// </summary>
    [JsonConverter(typeof(NullableStringOrNumberToIntJsonConverter))]
    [JsonPropertyName("definition_n")]
    public int? DefinitionNew { get; init; }

    /// <summary>
    /// 获取或设置名称。
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }
}
