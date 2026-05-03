using System.Text.Json;

using Microsoft.Extensions.Caching.Distributed;

namespace Infrastructure.Caching;

/// <summary>
/// <see cref="IDistributedCache"/> 的 JSON 序列化扩展方法。
/// </summary>
/// <remarks>
/// 按设计 §3. IDistributedCache 封装实现：<br/>
/// - 使用 <see cref="System.Text.Json"/> 进行序列化/反序列化（R7.5）<br/>
/// - 提供 <see cref="GetAsync{T}"/>、<see cref="SetAsync{T}"/>、<see cref="GetOrSetAsync{T}"/>（R7.1、R7.2）<br/>
/// - 过期时间可由调用方显式传入，未传入时由调用方根据 <see cref="CachingOptions.DefaultTtl"/> 决定（R7.3）。
/// </remarks>
public static class DistributedCacheExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// 按键读取并反序列化缓存值。缓存未命中时返回 <see langword="default"/>。
    /// </summary>
    public static async Task<T?> GetAsync<T>(
        this IDistributedCache cache,
        string key,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        byte[]? bytes = await cache.GetAsync(key, cancellationToken);
        if (bytes is null || bytes.Length == 0)
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(bytes, JsonOptions);
    }

    /// <summary>
    /// 将对象序列化为 JSON 字节后写入缓存。
    /// </summary>
    /// <param name="cache">缓存实例。</param>
    /// <param name="key">缓存键。</param>
    /// <param name="value">要写入的值，可为 <see langword="null"/>。</param>
    /// <param name="ttl">相对过期时间；<see langword="null"/> 时由调用方选择是否使用默认 TTL。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public static async Task SetAsync<T>(
        this IDistributedCache cache,
        string key,
        T value,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);

        var options = new DistributedCacheEntryOptions();
        if (ttl.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = ttl.Value;
        }

        await cache.SetAsync(key, bytes, options, cancellationToken);
    }

    /// <summary>
    /// 缓存命中时直接返回；未命中则调用 <paramref name="factory"/>，将结果写入缓存后返回。
    /// </summary>
    /// <param name="cache">缓存实例。</param>
    /// <param name="key">缓存键。</param>
    /// <param name="factory">未命中时用于生成值的异步工厂。</param>
    /// <param name="ttl">相对过期时间；<see langword="null"/> 时使用缓存服务默认值。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>命中时返回反序列化后的值；未命中时返回 <paramref name="factory"/> 生成的值。</returns>
    public static async Task<T?> GetOrSetAsync<T>(
        this IDistributedCache cache,
        string key,
        Func<CancellationToken, Task<T?>> factory,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        byte[]? bytes = await cache.GetAsync(key, cancellationToken);
        if (bytes is not null && bytes.Length > 0)
        {
            // 命中：直接返回反序列化结果（即使原值是 null 也视为命中）
            return JsonSerializer.Deserialize<T>(bytes, JsonOptions);
        }

        T? value = await factory(cancellationToken);

        byte[] newBytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
        var options = new DistributedCacheEntryOptions();
        if (ttl.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = ttl.Value;
        }

        await cache.SetAsync(key, newBytes, options, cancellationToken);
        return value;
    }
}
