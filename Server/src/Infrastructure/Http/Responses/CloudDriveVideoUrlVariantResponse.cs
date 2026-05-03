using System.Text.Json;
using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

/// <summary>
/// 115 视频地址变体响应模型。
/// </summary>
[JsonConverter(typeof(CloudDriveVideoUrlVariantJsonConverter))]
internal sealed class CloudDriveVideoUrlVariantResponse
{
    /// <summary>
    /// 获取或设置视频清晰度值。
    /// </summary>
    public int? Definition { get; init; }

    /// <summary>
    /// 获取或设置新版视频清晰度值。
    /// </summary>
    public int? DefinitionNew { get; init; }

    /// <summary>
    /// 获取或设置视频清晰度名称。
    /// </summary>
    public string? DefinitionName { get; init; }

    /// <summary>
    /// 获取或设置资源地址。
    /// </summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>
    /// 获取或设置文件大小。
    /// </summary>
    public long? Size { get; init; }

    /// <summary>
    /// 获取或设置视频高度。
    /// </summary>
    public int? Height { get; init; }

    /// <summary>
    /// 获取或设置视频宽度。
    /// </summary>
    public int? Width { get; init; }

    /// <summary>
    /// 获取或设置原始 JSON 对象。
    /// </summary>
    public JsonElement Raw { get; init; }
}
