namespace Infrastructure.Caching;

/// <summary>
/// 分布式缓存框架级选项。
/// </summary>
public sealed class CachingOptions
{
    /// <summary>
    /// 配置节名称。
    /// </summary>
    public const string SectionName = "Caching";

    /// <summary>
    /// 缓存默认 TTL。调用方未显式指定过期时间时使用（R7.3）。
    /// </summary>
    public TimeSpan DefaultTtl { get; init; } = TimeSpan.FromMinutes(10);
}
