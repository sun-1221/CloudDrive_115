using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

/// <summary>
/// 115 面包屑路径项响应模型。
/// </summary>
internal sealed class CloudDrive115BreadcrumbItem
{
    /// <summary>
    /// 获取或设置目录标识。
    /// </summary>
    [JsonPropertyName("cid")]
    public string DirectoryId { get; init; } = string.Empty;

    /// <summary>
    /// 获取或设置名称。
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 获取或设置 115 空间分区标识。
    /// </summary>
    [JsonConverter(typeof(NullableStringOrNumberToLongJsonConverter))]
    [JsonPropertyName("aid")]
    public long? Aid { get; init; }

    /// <summary>
    /// 获取或设置当前路径是否被选中的原始值。
    /// </summary>
    [JsonPropertyName("isp")]
    public string IsSelectedPath { get; init; } = string.Empty;

    /// <summary>
    /// 获取或设置父级目录标识。
    /// </summary>
    [JsonPropertyName("pid")]
    public string? ParentId { get; init; }
}
