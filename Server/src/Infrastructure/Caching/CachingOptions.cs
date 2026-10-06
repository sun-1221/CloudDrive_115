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
    /// 业务缓存封装可选用的默认 TTL；基础 Cache_Extensions 不会隐式读取该值（R7.3）。
    /// </summary>
    public TimeSpan DefaultTtl { get; init; } = TimeSpan.FromMinutes(10);
}
