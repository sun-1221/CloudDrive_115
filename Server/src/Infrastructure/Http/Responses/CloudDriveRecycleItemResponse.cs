using System.Text.Json;
using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

/// <summary>
/// 115 回收站条目响应模型。
/// </summary>
[JsonConverter(typeof(CloudDriveRecycleItemJsonConverter))]
internal sealed class CloudDriveRecycleItemResponse
{
    /// <summary>
    /// 获取或设置标识。
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// 获取或设置文件名称。
    /// </summary>
    public string FileName { get; init; } = string.Empty;

    /// <summary>
    /// 获取或设置 115 返回的类型值。
    /// </summary>
    public int? Type { get; init; }

    /// <summary>
    /// 获取或设置当前条目是否为目录。
    /// </summary>
    public bool IsDirectory { get; init; }

    /// <summary>
    /// 获取或设置文件大小。
    /// </summary>
    public long FileSize { get; init; }

    /// <summary>
    /// 获取或设置删除时间戳。
    /// </summary>
    public long? DeletedTime { get; init; }

    /// <summary>
    /// 获取或设置缩略图地址。
    /// </summary>
    public string? ThumbnailUrl { get; init; }

    /// <summary>
    /// 获取或设置原父级目录标识。
    /// </summary>
    public string? OriginalParentId { get; init; }

    /// <summary>
    /// 获取或设置原父级目录名称。
    /// </summary>
    public string? OriginalParentName { get; init; }

    /// <summary>
    /// 获取或设置 115 文件提取码。
    /// </summary>
    public string? PickCode { get; init; }

    /// <summary>
    /// 获取或设置还原状态。
    /// </summary>
    public int? RestoreStatus { get; init; }

    /// <summary>
    /// 获取或设置原始 JSON 对象。
    /// </summary>
    public JsonElement Raw { get; init; }
}
