using System.Collections.Concurrent;

using Application.Abstractions.CloudDrive;
using Application.CloudDrive;

using Infrastructure.Caching;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Infrastructure.CloudDrive;

internal sealed class CloudDriveMetadataCache(
    IDistributedCache cache,
    IOptions<CloudDriveAppConfig> configOptions) : ICloudDriveMetadataCache
{
    private readonly ConcurrentDictionary<string, byte> _knownKeys = new(StringComparer.Ordinal);

    public async Task<T?> GetOrSetAsync<T>(
        string cacheKey,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheKey);
        ArgumentNullException.ThrowIfNull(factory);
        ThrowIfSensitiveCacheKey(cacheKey);

        TimeSpan effectiveTtl = ttl ?? TimeSpan.FromSeconds(configOptions.Value.CacheTtlSeconds);
        T? value = await cache.GetOrSetAsync(
            cacheKey,
            async token => await factory(token),
            effectiveTtl,
            cancellationToken);
        _knownKeys.TryAdd(cacheKey, 0);
        return value;
    }

    public async Task InvalidateDirectoryAsync(
        Guid userId,
        string cloudAccountUid,
        string directoryId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cloudAccountUid);
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryId);

        foreach (string key in FindKeys(BuildDirectoryPrefix(userId, cloudAccountUid, directoryId)))
        {
            await cache.RemoveAsync(key, cancellationToken);
            _knownKeys.TryRemove(key, out _);
        }
    }

    public async Task InvalidateFileAsync(
        Guid userId,
        string cloudAccountUid,
        string fileId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cloudAccountUid);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        foreach (string key in FindKeys(BuildFilePrefix(userId, cloudAccountUid, fileId)))
        {
            await cache.RemoveAsync(key, cancellationToken);
            _knownKeys.TryRemove(key, out _);
        }
    }

    internal static string BuildDirectoryKey(Guid userId, string cloudAccountUid, string directoryId) =>
        $"{BuildDirectoryPrefix(userId, cloudAccountUid, directoryId)}:default";

    internal static string BuildFileKey(Guid userId, string cloudAccountUid, string fileId) =>
        $"{BuildFilePrefix(userId, cloudAccountUid, fileId)}:default";

    internal static string BuildDirectoryPrefix(Guid userId, string cloudAccountUid, string directoryId) =>
        $"cloud-drive:metadata:dir:{userId:N}:{cloudAccountUid}:{directoryId}";

    internal static string BuildFilePrefix(Guid userId, string cloudAccountUid, string fileId) =>
        $"cloud-drive:metadata:file:{userId:N}:{cloudAccountUid}:{fileId}";

    internal int KnownKeyCount => _knownKeys.Count;

    private IReadOnlyList<string> FindKeys(string prefix)
    {
        return _knownKeys.Keys
            .Where(key => key.StartsWith(prefix, StringComparison.Ordinal))
            .ToList();
    }

    private static void ThrowIfSensitiveCacheKey(string cacheKey)
    {
        string[] sensitiveFragments =
        [
            "cookie",
            "uid=",
            "cid=",
            "ussid",
            "seid",
            "sign=",
            "signkey",
            "password",
            "encryptedcookiepayload",
            "extrafields"
        ];

        if (sensitiveFragments.Any(fragment => cacheKey.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("CloudDrive metadata cache key contains sensitive field names.");
        }
    }
}
