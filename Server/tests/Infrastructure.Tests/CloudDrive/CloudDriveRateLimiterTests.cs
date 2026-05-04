using Application.CloudDrive;

using Domain.Exceptions;

using Infrastructure.CloudDrive;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Tests.CloudDrive;

public sealed class CloudDriveRateLimiterTests
{
    [Fact]
    public async Task AcquireAsync_WhenRejectStrategyAndNoToken_ThrowsRateLimitAsync()
    {
        // Arrange
        var logger = new RecordingRateLimiterLogger();
        CloudDriveRateLimiter limiter = CreateLimiter(new CloudDriveAppConfig
        {
            DefaultRateLimit = 1,
            RateLimitOverflowStrategy = RateLimitOverflowStrategy.Reject,
        }, logger);
        Guid userId = Guid.NewGuid();

        using IDisposable first = await limiter.AcquireAsync(userId, "115001", "files", CancellationToken.None);

        // Act
        CloudDriveRateLimitException exception = await Assert.ThrowsAsync<CloudDriveRateLimitException>(
            () => limiter.AcquireAsync(userId, "115001", "files", CancellationToken.None));

        // Assert
        Assert.Equal("cloud_drive_local_rate_limited", exception.ErrorCode);
        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Warning
            && entry.Message.Contains("115 本地限流拒绝请求", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AcquireAsync_WhenDifferentCategory_UsesDifferentBucketAsync()
    {
        // Arrange
        CloudDriveRateLimiter limiter = CreateLimiter(new CloudDriveAppConfig
        {
            DefaultRateLimit = 1,
            RateLimitOverflowStrategy = RateLimitOverflowStrategy.Reject,
        });
        Guid userId = Guid.NewGuid();

        using IDisposable first = await limiter.AcquireAsync(userId, "115001", "files", CancellationToken.None);

        // Act
        using IDisposable second = await limiter.AcquireAsync(userId, "115001", "video", CancellationToken.None);

        // Assert
        Assert.Equal(2, limiter.BucketCount);
    }

    [Fact]
    public async Task AcquireAsync_WhenQueueStrategy_WaitsForRefillAsync()
    {
        // Arrange
        CloudDriveRateLimiter limiter = CreateLimiter(new CloudDriveAppConfig
        {
            DefaultRateLimit = 5,
            RateLimitOverflowStrategy = RateLimitOverflowStrategy.Queue,
            RateLimitMaxQueueLength = 1,
        });
        Guid userId = Guid.NewGuid();

        List<IDisposable> permits = [];
        for (int i = 0; i < 5; i++)
        {
            permits.Add(await limiter.AcquireAsync(userId, "115001", "files", CancellationToken.None));
        }

        // Act
        using IDisposable queued = await limiter.AcquireAsync(userId, "115001", "files", CancellationToken.None);

        // Assert
        Assert.NotNull(queued);

        foreach (IDisposable permit in permits)
        {
            permit.Dispose();
        }
    }

    [Fact]
    public async Task RecordThrottleSignal_WhenCalled_ForcesBackoffAsync()
    {
        // Arrange
        var logger = new RecordingRateLimiterLogger();
        CloudDriveRateLimiter limiter = CreateLimiter(new CloudDriveAppConfig
        {
            DefaultRateLimit = 10,
            RateLimitOverflowStrategy = RateLimitOverflowStrategy.Queue,
            RateLimitMaxQueueLength = 1,
            ThrottleRetryBaseDelayMilliseconds = 100,
            ThrottleRetryMaxDelayMilliseconds = 100,
        }, logger);
        Guid userId = Guid.NewGuid();

        limiter.RecordThrottleSignal(userId, "115001", "files");

        DateTime before = DateTime.UtcNow;

        // Act
        using IDisposable permit = await limiter.AcquireAsync(userId, "115001", "files", CancellationToken.None);

        // Assert
        Assert.True(DateTime.UtcNow - before >= TimeSpan.FromMilliseconds(75));
        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Warning
            && entry.Message.Contains("115 上游频控信号已记录", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AcquireAsync_WhenQueueIsFull_ThrowsRateLimitAsync()
    {
        // Arrange
        var logger = new RecordingRateLimiterLogger();
        CloudDriveRateLimiter limiter = CreateLimiter(new CloudDriveAppConfig
        {
            DefaultRateLimit = 1,
            RateLimitOverflowStrategy = RateLimitOverflowStrategy.Queue,
            RateLimitMaxQueueLength = 0,
        }, logger);
        Guid userId = Guid.NewGuid();

        using IDisposable first = await limiter.AcquireAsync(userId, "115001", "files", CancellationToken.None);

        // Act
        CloudDriveRateLimitException exception = await Assert.ThrowsAsync<CloudDriveRateLimitException>(
            () => limiter.AcquireAsync(userId, "115001", "files", CancellationToken.None));

        // Assert
        Assert.Equal("cloud_drive_local_rate_limited", exception.ErrorCode);
        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Warning
            && entry.Message.Contains("115 本地限流拒绝请求", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Reset_WhenAccountHasBuckets_RemovesMatchingBucketsAsync()
    {
        // Arrange
        CloudDriveRateLimiter limiter = CreateLimiter(new CloudDriveAppConfig
        {
            DefaultRateLimit = 1,
            RateLimitOverflowStrategy = RateLimitOverflowStrategy.Reject,
        });
        Guid userId = Guid.NewGuid();

        using IDisposable files = await limiter.AcquireAsync(userId, "115001", "files", CancellationToken.None);
        using IDisposable video = await limiter.AcquireAsync(userId, "115001", "video", CancellationToken.None);
        using IDisposable other = await limiter.AcquireAsync(userId, "115002", "files", CancellationToken.None);

        // Act
        limiter.Reset(userId, "115001");

        // Assert
        Assert.Equal(1, limiter.BucketCount);
    }

    private static CloudDriveRateLimiter CreateLimiter(
        CloudDriveAppConfig config,
        ILogger<CloudDriveRateLimiter>? logger = null) =>
        new(Options.Create(config), logger ?? new RecordingRateLimiterLogger());

    private sealed class RecordingRateLimiterLogger : ILogger<CloudDriveRateLimiter>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new LogEntry(logLevel, formatter(state, exception)));
        }
    }

    private sealed record LogEntry(LogLevel Level, string Message);
}
