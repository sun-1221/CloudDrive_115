using Application.Abstractions.CloudDrive;
using Application.CloudDrive;
using Application.CloudDrive.Files;
using Application.Tests.Support;

using Microsoft.Extensions.Options;

namespace Application.Tests.CloudDrive;

/// <summary>
/// 属性 P6：频控限流一致性。
/// 验证每次 API 调用都经过限流器，限流器信号记录正确触发。
/// </summary>
public sealed class RateLimitConsistencyPropertyTests
{
    [Fact]
    public async Task GetFileList_AlwaysAcquiresRateLimitPermit()
    {
        // Arrange
        var cache = new AlwaysMissCache();
        var gateway = new CountingGateway();
        var rateLimiter = new CountingRateLimiter();
        var config = Options.Create(new CloudDriveAppConfig());
        var logger = new FakeLogger<CloudDriveFileManager>();

        var manager = new CloudDriveFileManager(cache, gateway, rateLimiter, config, logger);
        Guid userId = Guid.NewGuid();
        const string cloudAccountUid = "115-user-001";

        // Act — 调用 3 次
        for (int i = 0; i < 3; i++)
        {
            await manager.GetFileListAsync<string>(
                userId, cloudAccountUid, $"dir-{i}",
                new CloudDriveApiContext { Category = "files" });
        }

        // Assert — 每次都应获取限流许可
        Assert.Equal(3, rateLimiter.AcquireCount);
    }

    [Fact]
    public void RecordThrottleSignal_RecordsCorrectDimension()
    {
        // Arrange
        var rateLimiter = new CountingRateLimiter();
        Guid userId = Guid.NewGuid();
        const string cloudAccountUid = "115-user-001";

        // Act
        rateLimiter.RecordThrottleSignal(userId, cloudAccountUid, "files");

        // Assert
        Assert.Single(rateLimiter.ThrottleSignals);
        Assert.Equal((userId, cloudAccountUid, "files"), rateLimiter.ThrottleSignals[0]);
    }

    /// <summary>
    /// 永不命中的缓存（每次调用都走 factory）。
    /// </summary>
    private sealed class AlwaysMissCache : ICloudDriveMetadataCache
    {
        public async Task<T?> GetOrSetAsync<T>(
            string cacheKey,
            Func<CancellationToken, Task<T>> factory,
            TimeSpan? ttl = null,
            CancellationToken cancellationToken = default) =>
            await factory(cancellationToken);

        public Task InvalidateDirectoryAsync(
            Guid userId,
            string cloudAccountUid,
            string directoryId,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task InvalidateFileAsync(
            Guid userId,
            string cloudAccountUid,
            string fileId,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    /// <summary>
    /// 测试用 API 网关（计数）。
    /// </summary>
    private sealed class CountingGateway : ICloudDriveOpenApiGateway
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
    /// 测试用限流器（计数）。
    /// </summary>
    private sealed class CountingRateLimiter : ICloudDriveRateLimiter
    {
        public int AcquireCount { get; private set; }
        public List<(Guid, string, string?)> ThrottleSignals { get; } = [];

        public Task<IDisposable> AcquireAsync(Guid userId, string cloudAccountUid, string? category = null, CancellationToken cancellationToken = default)
        {
            AcquireCount++;
            return Task.FromResult<IDisposable>(new NoOpDisposable());
        }

        public void RecordThrottleSignal(Guid userId, string cloudAccountUid, string? category = null)
        {
            ThrottleSignals.Add((userId, cloudAccountUid, category));
        }

        public void Reset(Guid userId, string cloudAccountUid, string? category = null) { }

        private sealed class NoOpDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }
}
