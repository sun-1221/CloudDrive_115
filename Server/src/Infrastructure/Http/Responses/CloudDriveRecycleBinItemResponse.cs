using System.Text.Json.Serialization;

namespace Infrastructure.Http.Responses;

/// <summary>
/// 115 回收站条目的已采集字符串字段。
/// </summary>
internal sealed class CloudDriveRecycleBinItemResponse
{
    /// <summary>获取或设置目录 ID。</summary>
    [JsonPropertyName("cid")]
    public string? Cid { get; init; }

    /// <summary>获取或设置删除时间原始值。</summary>
    [JsonPropertyName("dtime")]
    public string? DeleteTime { get; init; }

    /// <summary>获取或设置文件名。</summary>
    [JsonPropertyName("file_name")]
    public string? FileName { get; init; }

    /// <summary>获取或设置文件大小原始值。</summary>
    [JsonPropertyName("file_size")]
    public string? FileSize { get; init; }

    /// <summary>获取或设置回收站条目标识。</summary>
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    /// <summary>获取或设置父目录名称。</summary>
    [JsonPropertyName("parent_name")]
    public string? ParentName { get; init; }

    /// <summary>获取或设置回收状态原始值。</summary>
    [JsonPropertyName("status")]
    public string? Status { get; init; }

    /// <summary>获取或设置条目类型原始值。</summary>
    [JsonPropertyName("type")]
    public string? Type { get; init; }
}
