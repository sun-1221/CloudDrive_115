using FsCheck.Xunit;

using Infrastructure.Caching;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Infrastructure.Tests.Caching;

/// <summary>
/// 设计文档 Property 8 与 Property 9：DistributedCache 扩展方法属性测试。
/// </summary>
public sealed class DistributedCacheExtensionsPropertyTests
{
    private static IDistributedCache CreateCache()
    {
        IOptions<MemoryDistributedCacheOptions> opts =
            Options.Create(new MemoryDistributedCacheOptions());
        return new MemoryDistributedCache(opts);
    }

    /// <summary>
    /// Property 8：对任意可序列化对象，<c>SetAsync</c> 后 <c>GetAsync</c> 返回等价对象。
    /// </summary>
    [Property(MaxTest = 30)]
    public async Task<bool> SetAsync_ThenGetAsync_ReturnsEquivalentObject(int id, int age)
    {
        // Arrange
        IDistributedCache cache = CreateCache();
        string key = $"k-{Guid.NewGuid()}";
        var original = new TestEntry(id, $"name-{age}", age);

        // Act
        await cache.SetAsync(key, original);
        TestEntry? loaded = await cache.GetAsync<TestEntry>(key);

        // Assert：record 的结构相等性
        return loaded is not null && loaded == original;
    }

    /// <summary>
    /// Property 9：首次 <c>GetOrSetAsync</c> 调用 factory 一次并返回结果；
    /// 同一键再次调用 factory 不被调用，直接返回缓存中的值。
    /// </summary>
    [Property(MaxTest = 30)]
    public async Task<bool> GetOrSetAsync_CallsFactoryOnceAndReusesValue(int payload)
    {
        // Arrange
        IDistributedCache cache = CreateCache();
        string key = $"k-{Guid.NewGuid()}";
        int factoryCalls = 0;
        var expected = new TestEntry(payload, $"n-{payload}", payload);

        Task<TestEntry?> Factory(CancellationToken _)
        {
            factoryCalls++;
            return Task.FromResult<TestEntry?>(expected);
        }

        // Act
        TestEntry? first = await cache.GetOrSetAsync<TestEntry>(key, Factory);
        TestEntry? second = await cache.GetOrSetAsync<TestEntry>(key, Factory);

        // Assert
        return factoryCalls == 1
            && first is not null
            && second is not null
            && first == expected
            && second == expected;
    }

    [Fact]
    public async Task GetAsync_WhenKeyMissing_ReturnsDefault()
    {
        // Arrange
        IDistributedCache cache = CreateCache();

        // Act
        TestEntry? loaded = await cache.GetAsync<TestEntry>("missing-key");

        // Assert
        Assert.Null(loaded);
    }

    [Fact]
    public async Task SetAsync_WithTtl_PersistsEntryUntilExpiration()
    {
        // Arrange
        IDistributedCache cache = CreateCache();
        string key = $"k-{Guid.NewGuid()}";
        var original = new TestEntry(1, "x", 10);

        // Act
        await cache.SetAsync(key, original, TimeSpan.FromSeconds(10));
        TestEntry? loaded = await cache.GetAsync<TestEntry>(key);

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal(original, loaded);
    }

    public sealed record TestEntry(int Id, string Name, int Age);
}
