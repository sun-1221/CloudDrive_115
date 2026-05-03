using System.Text.Json;

namespace Infrastructure.Http.Json;

/// <summary>
/// 115 HTTP 响应反序列化选项工厂。
/// </summary>
internal static class CloudDrive115JsonSerializerOptions
{
    /// <summary>
    /// 创建 115 HTTP 响应反序列化默认选项。
    /// </summary>
    internal static JsonSerializerOptions Defaults { get; } = CreateDefaults();

    /// <summary>
    /// 创建 115 HTTP 响应反序列化默认选项。
    /// </summary>
    private static JsonSerializerOptions CreateDefaults()
    {
        return new JsonSerializerOptions
        {
            // 115 同时存在大小写不同但语义不同的字段，关闭大小写不敏感避免错误字段被吞并。
            PropertyNameCaseInsensitive = false,
        };
    }
}
