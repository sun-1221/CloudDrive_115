using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Caching;

/// <summary>
/// 分布式缓存基础设施注册入口。
/// </summary>
public static class CachingSetup
{
    /// <summary>
    /// 注册分布式内存缓存（R7.4）。生产环境可叠加 <c>AddStackExchangeRedisCache()</c> 替换为 Redis 实现。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <returns>原始服务集合，支持链式调用。</returns>
    public static IServiceCollection AddCaching(this IServiceCollection services)
    {
        services.AddDistributedMemoryCache();
        return services;
    }
}
