using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Caching;

/// <summary>
/// 分布式缓存基础设施注册入口。
/// </summary>
public static class CachingSetup
{
    /// <summary>
    /// 注册开发环境默认的分布式内存缓存（R7.4）。生产环境可按部署需要另行引入 Redis 等 <c>IDistributedCache</c> 提供程序。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <returns>原始服务集合，支持链式调用。</returns>
    public static IServiceCollection AddCaching(this IServiceCollection services)
    {
        // 默认注册内存实现，生产环境可在组合根中追加 Redis 等 IDistributedCache 实现进行覆盖。
        services.AddDistributedMemoryCache();
        return services;
    }
}
