using Application.CloudDrive;

using Domain.Exceptions;

using Infrastructure.CloudDrive;

using Microsoft.Extensions.Options;

namespace Infrastructure.Tests.CloudDrive;

public sealed class CloudDriveRateLimiterTests
{
    [Fact]
    public async Task AcquireAsync_WhenRejectStrategyAndNoToken_ThrowsRateLimitAsync()
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
        CloudDriveRateLimitException exception = await Assert.ThrowsAsync<CloudDriveRateLimitException>(
            () => limiter.AcquireAsync(userId, "115001", "files", CancellationToken.None));

        // Assert
        Assert.Equal("cloud_drive_local_rate_limited", exception.ErrorCode);
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
        CloudDriveRateLimiter limiter = CreateLimiter(new CloudDriveAppConfig
        {
            DefaultRateLimit = 10,
            RateLimitOverflowStrategy = RateLimitOverflowStrategy.Queue,
            RateLimitMaxQueueLength = 1,
            ThrottleRetryBaseDelayMilliseconds = 100,
            ThrottleRetryMaxDelayMilliseconds = 100,
        });
        Guid userId = Guid.NewGuid();

        limiter.RecordThrottleSignal(userId, "115001", "files");

        DateTime before = DateTime.UtcNow;

        // Act
        using IDisposable permit = await limiter.AcquireAsync(userId, "115001", "files", CancellationToken.None);

        // Assert
        Assert.True(DateTime.UtcNow - before >= TimeSpan.FromMilliseconds(75));
    }

    [Fact]
    public async Task AcquireAsync_WhenQueueIsFull_ThrowsRateLimitAsync()
    {
        // Arrange
        CloudDriveRateLimiter limiter = CreateLimiter(new CloudDriveAppConfig
        {
            DefaultRateLimit = 1,
            RateLimitOverflowStrategy = RateLimitOverflowStrategy.Queue,
            RateLimitMaxQueueLength = 0,
        });
        Guid userId = Guid.NewGuid();

        using IDisposable first = await limiter.AcquireAsync(userId, "115001", "files", CancellationToken.None);

        // Act
        CloudDriveRateLimitException exception = await Assert.ThrowsAsync<CloudDriveRateLimitException>(
            () => limiter.AcquireAsync(userId, "115001", "files", CancellationToken.None));

        // Assert
        Assert.Equal("cloud_drive_local_rate_limited", exception.ErrorCode);
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

    private static CloudDriveRateLimiter CreateLimiter(CloudDriveAppConfig config) =>
        new(Options.Create(config));
}
