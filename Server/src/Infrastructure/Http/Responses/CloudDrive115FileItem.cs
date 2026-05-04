using System.Text.Json;
using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

/// <summary>
/// 115 文件或目录条目响应模型。
/// </summary>
[JsonConverter(typeof(CloudDrive115FileItemJsonConverter))]
internal sealed class CloudDrive115FileItem
{
    /// <summary>
    /// 获取或设置文件标识。
    /// </summary>
    public string FileId { get; init; } = string.Empty;

    /// <summary>
    /// 获取或设置名称。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 获取或设置文件大小。
    /// </summary>
    public long Size { get; init; }

    /// <summary>
    /// 获取或设置 115 文件分类值。
    /// </summary>
    public int? FileCategory { get; init; }

    /// <summary>
    /// 获取或设置当前条目是否为目录。
    /// </summary>
    public bool? IsDirectory { get; init; }

    /// <summary>
    /// 获取或设置 115 文件提取码。
    /// </summary>
    public string? PickCode { get; init; }

    /// <summary>
    /// 获取或设置缩略图地址。
    /// </summary>
    public string? ThumbnailUrl { get; init; }

    /// <summary>
    /// 获取或设置原始资源地址。
    /// </summary>
    public string? OriginalUrl { get; init; }

    /// <summary>
    /// 获取或设置视频封面地址。
    /// </summary>
    public string? VideoImageUrl { get; init; }

    /// <summary>
    /// 获取或设置当前条目是否为视频。
    /// </summary>
    public bool? IsVideo { get; init; }

    /// <summary>
    /// 获取或设置视频清晰度标识。
    /// </summary>
    public string? VideoDefinition { get; init; }

    /// <summary>
    /// 获取或设置播放时长。
    /// </summary>
    public long? PlayDuration { get; init; }

    /// <summary>
    /// 获取或设置上传时间戳。
    /// </summary>
    public long? UploadTime { get; init; }

    /// <summary>
    /// 获取或设置更新时间戳。
    /// </summary>
    public long? UpdateTime { get; init; }

    /// <summary>
    /// 获取或设置是否已标星。
    /// </summary>
    public bool? IsStarred { get; init; }

    /// <summary>
    /// 获取或设置是否为私密文件。
    /// </summary>
    public bool? IsPrivate { get; init; }

    /// <summary>
    /// 获取或设置上传状态。
    /// </summary>
    public int? UploadStatus { get; init; }

    /// <summary>
    /// 获取或设置文件 SHA1。
    /// </summary>
    public string? Sha1 { get; init; }

    /// <summary>
    /// 获取或设置文件图标标识。
    /// </summary>
    public string? Icon { get; init; }

    /// <summary>
    /// 获取或设置文件分类名称。
    /// </summary>
    public string? FileClass { get; init; }

    /// <summary>
    /// 获取或设置原始 JSON 对象。
    /// </summary>
    public JsonElement Raw { get; init; }
}
