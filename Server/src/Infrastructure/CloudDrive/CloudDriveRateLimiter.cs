using System.Collections.Concurrent;

using Application.Abstractions.CloudDrive;
using Application.CloudDrive;

using Domain.Exceptions;

using Infrastructure.Diagnostics;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.CloudDrive;

internal sealed class CloudDriveRateLimiter(
    IOptions<CloudDriveAppConfig> configOptions,
    ICloudDriveSensitiveFieldsFilter sensitiveFieldsFilter,
    ILogger<CloudDriveRateLimiter> logger) : ICloudDriveRateLimiter
{
    private readonly ConcurrentDictionary<string, TokenBucket> _buckets = new(StringComparer.Ordinal);

    public Task<IDisposable> AcquireAsync(
        Guid userId,
        string cloudAccountUid,
        string? category = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cloudAccountUid);
        cancellationToken.ThrowIfCancellationRequested();

        CloudDriveAppConfig config = configOptions.Value;
        string normalizedCategory = NormalizeCategory(category);
        string key = BuildKey(userId, cloudAccountUid, normalizedCategory);
        TokenBucket bucket = _buckets.GetOrAdd(
            key,
            _ => new TokenBucket(GetLimit(config, normalizedCategory), config.RateLimitMaxQueueLength));

        return bucket.AcquireAsync(
            config,
            waitDelay =>
            {
                CloudDriveMetrics.RecordRateLimitQueued(normalizedCategory);
                logger.LogInformation(
                    "115 本地限流排队 UserId={UserId}, CloudAccountUid={CloudAccountUid}, Category={Category}, DelayMs={DelayMs}",
                    userId,
                    sensitiveFieldsFilter.Redact(cloudAccountUid),
                    normalizedCategory,
                    waitDelay.TotalMilliseconds);
            },
            () =>
            {
                CloudDriveMetrics.RecordRateLimitRejected(normalizedCategory);
                logger.LogWarning(
                    "115 本地限流拒绝请求 UserId={UserId}, CloudAccountUid={CloudAccountUid}, Category={Category}",
                    userId,
                    sensitiveFieldsFilter.Redact(cloudAccountUid),
                    normalizedCategory);
            },
            cancellationToken);
    }

    public void RecordThrottleSignal(Guid userId, string cloudAccountUid, string? category = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cloudAccountUid);

        CloudDriveAppConfig config = configOptions.Value;
        string normalizedCategory = NormalizeCategory(category);
        string key = BuildKey(userId, cloudAccountUid, normalizedCategory);
        TokenBucket bucket = _buckets.GetOrAdd(
            key,
            _ => new TokenBucket(GetLimit(config, normalizedCategory), config.RateLimitMaxQueueLength));

        bucket.RecordThrottleSignal(config);
        CloudDriveMetrics.RecordRateLimitThrottleSignal(normalizedCategory);
        logger.LogWarning(
            "115 上游频控信号已记录 UserId={UserId}, CloudAccountUid={CloudAccountUid}, Category={Category}",
            userId,
            sensitiveFieldsFilter.Redact(cloudAccountUid),
            normalizedCategory);
    }

    public void Reset(Guid userId, string cloudAccountUid, string? category = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cloudAccountUid);

        if (!string.IsNullOrWhiteSpace(category))
        {
            _buckets.TryRemove(BuildKey(userId, cloudAccountUid, NormalizeCategory(category)), out _);
            return;
        }

        string prefix = $"{userId:N}:{cloudAccountUid}:";
        foreach (string key in _buckets.Keys)
        {
            if (key.StartsWith(prefix, StringComparison.Ordinal))
            {
                _buckets.TryRemove(key, out _);
            }
        }
    }

    internal int BucketCount => _buckets.Count;

    private static string BuildKey(Guid userId, string cloudAccountUid, string category) =>
        $"{userId:N}:{cloudAccountUid}:{category}";

    private static string NormalizeCategory(string? category) =>
        string.IsNullOrWhiteSpace(category) ? "default" : category.Trim().ToLowerInvariant();

    private static int GetLimit(CloudDriveAppConfig config, string category)
    {
        if (config.RateLimitOverrides is not null
            && config.RateLimitOverrides.TryGetValue(category, out int overrideLimit)
            && overrideLimit > 0)
        {
            return overrideLimit;
        }

        return Math.Max(1, config.DefaultRateLimit);
    }

    private sealed class TokenBucket
    {
        private readonly object _syncRoot = new();
        private readonly int _capacity;
        private double _tokens;
        private DateTime _lastRefillUtc;
        private int _queuedCount;
        private DateTime? _backoffUntilUtc;

        public TokenBucket(int limitPerSecond, int maxQueueLength)
        {
            _capacity = Math.Max(1, limitPerSecond);
            MaxQueueLength = Math.Max(0, maxQueueLength);
            _tokens = _capacity;
            _lastRefillUtc = DateTime.UtcNow;
        }

        public int MaxQueueLength { get; }

        public async Task<IDisposable> AcquireAsync(
            CloudDriveAppConfig config,
            Action<TimeSpan> onQueued,
            Action onRejected,
            CancellationToken cancellationToken)
        {
            while (true)
            {
                TimeSpan waitDelay;
                bool queued = false;

                lock (_syncRoot)
                {
                    Refill(DateTime.UtcNow);

                    if (DateTime.UtcNow < _backoffUntilUtc)
                    {
                        waitDelay = _backoffUntilUtc.Value - DateTime.UtcNow;
                    }
                    else if (_tokens >= 1)
                    {
                        _tokens -= 1;
                        return NullPermit.Instance;
                    }
                    else
                    {
                        if (config.RateLimitOverflowStrategy == RateLimitOverflowStrategy.Reject
                            || _queuedCount >= MaxQueueLength)
                        {
                            onRejected();
                            throw new CloudDriveRateLimitException(
                                "115 网盘本地限流已触发",
                                "cloud_drive_local_rate_limited");
                        }

                        _queuedCount++;
                        queued = true;
                        waitDelay = TimeSpan.FromSeconds(1d / _capacity);
                    }
                }

                try
                {
                    if (queued)
                    {
                        onQueued(waitDelay);
                    }

                    await Task.Delay(ClampDelay(waitDelay), cancellationToken);
                }
                finally
                {
                    if (queued)
                    {
                        lock (_syncRoot)
                        {
                            _queuedCount = Math.Max(0, _queuedCount - 1);
                        }
                    }
                }
            }
        }

        public void RecordThrottleSignal(CloudDriveAppConfig config)
        {
            int delayMilliseconds = Math.Clamp(
                config.ThrottleRetryBaseDelayMilliseconds,
                1,
                Math.Max(1, config.ThrottleRetryMaxDelayMilliseconds));

            lock (_syncRoot)
            {
                _tokens = 0;
                _backoffUntilUtc = DateTime.UtcNow.AddMilliseconds(delayMilliseconds);
            }
        }

        private void Refill(DateTime nowUtc)
        {
            if (nowUtc <= _lastRefillUtc)
            {
                return;
            }

            double elapsedSeconds = (nowUtc - _lastRefillUtc).TotalSeconds;
            _tokens = Math.Min(_capacity, _tokens + elapsedSeconds * _capacity);
            _lastRefillUtc = nowUtc;
        }

        private static TimeSpan ClampDelay(TimeSpan delay)
        {
            if (delay <= TimeSpan.Zero)
            {
                return TimeSpan.Zero;
            }

            return delay > TimeSpan.FromSeconds(30) ? TimeSpan.FromSeconds(30) : delay;
        }
    }

    private sealed class NullPermit : IDisposable
    {
        public static NullPermit Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}
