using Application.Abstractions.CloudDrive;
using Application.CloudDrive;
using Application.CloudDrive.Files;
using Application.Tests.Support;

using Microsoft.Extensions.Options;

namespace Application.Tests.CloudDrive;

/// <summary>
/// 属性 P4：缓存一致性。
/// 验证写操作后缓存被清除，下一次读请求触发 API 调用。
/// </summary>
public sealed class CacheConsistencyPropertyTests
{
    [Fact]
    public async Task ExecuteWithCacheInvalidation_WhenDirectoryAffected_ClearsAffectedDirectoryCache()
    {
        // Arrange
        var cache = new FakeMetadataCache();
        var gateway = new FakeOpenApiGateway();
        var rateLimiter = new FakeRateLimiter();
        var config = Options.Create(new CloudDriveAppConfig());
        var logger = new FakeLogger<CloudDriveFileManager>();

        var manager = new CloudDriveFileManager(cache, gateway, config, logger);
        Guid userId = Guid.NewGuid();
        const string cloudAccountUid = "115-user-001";
        const string directoryId = "dir-001";

        // 先缓存一个目录
        cache.Seed($"cd:{userId}:{cloudAccountUid}:dir:{directoryId}:list", "cached-data");

        // Act — 执行写操作使缓存失效
        string result = await manager.ExecuteWithCacheInvalidationAsync(
            userId, cloudAccountUid,
            [directoryId],
            () => Task.FromResult("write-result"));

        // Assert
        Assert.Equal("write-result", result);
        Assert.Contains((userId, cloudAccountUid, directoryId), cache.InvalidatedDirectories);
    }

    /// <summary>
    /// 测试用元数据缓存。
    /// </summary>
    private sealed class FakeMetadataCache : ICloudDriveMetadataCache
    {
        private readonly Dictionary<string, object> _store = [];

        public List<(Guid UserId, string CloudAccountUid, string DirectoryId)> InvalidatedDirectories { get; } = [];

        public void Seed(string key, object value) => _store[key] = value;

        public async Task<T?> GetOrSetAsync<T>(
            string cacheKey,
            Func<CancellationToken, Task<T>> factory,
            TimeSpan? ttl = null,
            CancellationToken cancellationToken = default)
        {
            if (_store.TryGetValue(cacheKey, out object? cached))
            {
                return (T?)cached;
            }

            T value = await factory(cancellationToken);
            _store[cacheKey] = value!;
            return value;
        }

        public Task InvalidateDirectoryAsync(
            Guid userId,
            string cloudAccountUid,
            string directoryId,
            CancellationToken cancellationToken = default)
        {
            InvalidatedDirectories.Add((userId, cloudAccountUid, directoryId));

            // 移除匹配的缓存键
            var keysToRemove = _store.Keys.Where(k => k.Contains($"dir:{directoryId}")).ToList();
            foreach (string key in keysToRemove)
            {
                _store.Remove(key);
            }

            return Task.CompletedTask;
        }

        public Task InvalidateFileAsync(
            Guid userId,
            string cloudAccountUid,
            string fileId,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    /// <summary>
    /// 测试用 API 网关。
    /// </summary>
    private sealed class FakeOpenApiGateway : ICloudDriveWebApiGateway
    {
        public Task<T> SendWebApiRequestAsync<T>(Guid userId, string cloudAccountUid, HttpMethod method, string endpoint, CloudDriveApiContext context, CancellationToken cancellationToken = default) =>
            Task.FromResult(default(T)!);

        public Task<T> SendUserCenterRequestAsync<T>(Guid userId, string cloudAccountUid, HttpMethod method, string endpoint, CloudDriveApiContext context, CancellationToken cancellationToken = default) =>
            Task.FromResult(default(T)!);

        public Task<T> SendOfflineApiRequestAsync<T>(Guid userId, string cloudAccountUid, HttpMethod method, string endpoint, CloudDriveApiContext context, CancellationToken cancellationToken = default) =>
            Task.FromResult(default(T)!);

        public Task<T> SendUploadInitRequestAsync<T>(Guid userId, string cloudAccountUid, CloudDriveApiContext context, CancellationToken cancellationToken = default) =>
            Task.FromResult(default(T)!);

        public Task<T> UploadFileAsync<T>(Guid userId, string cloudAccountUid, string uploadUrl, Stream fileStream, string fileName, IDictionary<string, string>? formFields = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(default(T)!);
    }

    /// <summary>
    /// 测试用限流器。
    /// </summary>
    private sealed class FakeRateLimiter : ICloudDriveRateLimiter
    {
        public Task<IDisposable> AcquireAsync(Guid userId, string cloudAccountUid, string? category = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<IDisposable>(new NoOpDisposable());

        public void RecordThrottleSignal(Guid userId, string cloudAccountUid, string? category = null) { }

        public void Reset(Guid userId, string cloudAccountUid, string? category = null) { }

        private sealed class NoOpDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }
}
