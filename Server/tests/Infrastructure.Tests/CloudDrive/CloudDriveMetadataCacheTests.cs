using Application.CloudDrive;

using Infrastructure.CloudDrive;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Infrastructure.Tests.CloudDrive;

public sealed class CloudDriveMetadataCacheTests
{
    private readonly MemoryDistributedCache _distributedCache = new(Options.Create(new MemoryDistributedCacheOptions()));

    [Fact]
    public async Task GetOrSetAsync_WhenCalledTwice_UsesCachedValue()
    {
        // Arrange
        CloudDriveMetadataCache cache = CreateCache();
        Guid userId = Guid.NewGuid();
        string cacheKey = CloudDriveMetadataCache.BuildDirectoryKey(userId, "115001", "0");
        int factoryCalls = 0;

        // Act
        string? first = await cache.GetOrSetAsync(cacheKey, () =>
        {
            factoryCalls++;
            return Task.FromResult("official-data");
        });
        string? second = await cache.GetOrSetAsync(cacheKey, () =>
        {
            factoryCalls++;
            return Task.FromResult("new-data");
        });

        // Assert
        Assert.Equal("official-data", first);
        Assert.Equal("official-data", second);
        Assert.Equal(1, factoryCalls);
        Assert.Equal(1, cache.KnownKeyCount);
    }

    [Fact]
    public async Task InvalidateDirectoryAsync_RemovesOnlyMatchingUserAccountDirectoryKeys()
    {
        // Arrange
        CloudDriveMetadataCache cache = CreateCache();
        Guid userId = Guid.NewGuid();
        Guid otherUserId = Guid.NewGuid();
        string targetKey = $"{CloudDriveMetadataCache.BuildDirectoryPrefix(userId, "115001", "dir-1")}:list";
        string otherDirectoryKey = $"{CloudDriveMetadataCache.BuildDirectoryPrefix(userId, "115001", "dir-2")}:list";
        string otherUserKey = $"{CloudDriveMetadataCache.BuildDirectoryPrefix(otherUserId, "115001", "dir-1")}:list";

        await SeedAsync(cache, targetKey, "target");
        await SeedAsync(cache, otherDirectoryKey, "other-directory");
        await SeedAsync(cache, otherUserKey, "other-user");

        // Act
        await cache.InvalidateDirectoryAsync(userId, "115001", "dir-1");

        // Assert
        Assert.Equal("reloaded", await cache.GetOrSetAsync(targetKey, () => Task.FromResult("reloaded")));
        Assert.Equal("other-directory", await cache.GetOrSetAsync(otherDirectoryKey, () => Task.FromResult("miss")));
        Assert.Equal("other-user", await cache.GetOrSetAsync(otherUserKey, () => Task.FromResult("miss")));
    }

    [Fact]
    public async Task InvalidateFileAsync_RemovesOnlyMatchingFileKeys()
    {
        // Arrange
        CloudDriveMetadataCache cache = CreateCache();
        Guid userId = Guid.NewGuid();
        string fileKey = $"{CloudDriveMetadataCache.BuildFilePrefix(userId, "115001", "file-1")}:detail";
        string otherFileKey = $"{CloudDriveMetadataCache.BuildFilePrefix(userId, "115001", "file-2")}:detail";

        await SeedAsync(cache, fileKey, "file-1");
        await SeedAsync(cache, otherFileKey, "file-2");

        // Act
        await cache.InvalidateFileAsync(userId, "115001", "file-1");

        // Assert
        Assert.Equal("reloaded", await cache.GetOrSetAsync(fileKey, () => Task.FromResult("reloaded")));
        Assert.Equal("file-2", await cache.GetOrSetAsync(otherFileKey, () => Task.FromResult("miss")));
    }

    [Fact]
    public async Task GetOrSetAsync_WhenTtlExpires_ReloadsValue()
    {
        // Arrange
        CloudDriveMetadataCache cache = CreateCache(new CloudDriveAppConfig { CacheTtlSeconds = 1 });
        Guid userId = Guid.NewGuid();
        string cacheKey = CloudDriveMetadataCache.BuildDirectoryKey(userId, "115001", "0");
        int factoryCalls = 0;

        // Act
        _ = await cache.GetOrSetAsync(cacheKey, () =>
        {
            factoryCalls++;
            return Task.FromResult("first");
        }, TimeSpan.FromMilliseconds(50));
        await Task.Delay(80);
        string? reloaded = await cache.GetOrSetAsync(cacheKey, () =>
        {
            factoryCalls++;
            return Task.FromResult("second");
        }, TimeSpan.FromMilliseconds(50));

        // Assert
        Assert.Equal("second", reloaded);
        Assert.Equal(2, factoryCalls);
    }

    [Fact]
    public async Task GetOrSetAsync_WhenKeyContainsSensitiveField_Throws()
    {
        // Arrange
        CloudDriveMetadataCache cache = CreateCache();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => cache.GetOrSetAsync("cloud-drive:metadata:cookie:UID=secret", () => Task.FromResult("bad")));
    }

    private static async Task SeedAsync(CloudDriveMetadataCache cache, string key, string value)
    {
        _ = await cache.GetOrSetAsync(key, () => Task.FromResult(value));
    }

    private CloudDriveMetadataCache CreateCache(CloudDriveAppConfig? config = null)
    {
        return new CloudDriveMetadataCache(
            _distributedCache,
            Options.Create(config ?? new CloudDriveAppConfig { CacheTtlSeconds = 300 }));
    }
}
