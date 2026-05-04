using Application.Abstractions.CloudDrive;

namespace Application.Tests.Support;

internal sealed class FakeCloudDriveMetadataCache : ICloudDriveMetadataCache
{
    private readonly Dictionary<string, object?> _store = new(StringComparer.Ordinal);

    public List<string> GetOrSetKeys { get; } = [];

    public List<(Guid UserId, string CloudAccountUid, string DirectoryId)> InvalidatedDirectories { get; } = [];

    public List<(Guid UserId, string CloudAccountUid, string FileId)> InvalidatedFiles { get; } = [];

    public int FactoryCallCount { get; private set; }

    public void Seed(string cacheKey, object? value)
    {
        _store[cacheKey] = value;
    }

    public async Task<T?> GetOrSetAsync<T>(
        string cacheKey,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default)
    {
        GetOrSetKeys.Add(cacheKey);

        if (_store.TryGetValue(cacheKey, out object? cached))
        {
            return (T?)cached;
        }

        FactoryCallCount++;
        T value = await factory(cancellationToken);
        _store[cacheKey] = value;
        return value;
    }

    public Task InvalidateDirectoryAsync(
        Guid userId,
        string cloudAccountUid,
        string directoryId,
        CancellationToken cancellationToken = default)
    {
        InvalidatedDirectories.Add((userId, cloudAccountUid, directoryId));

        List<string> keysToRemove = _store.Keys
            .Where(key => key.Contains($":{cloudAccountUid}:dir:{directoryId}:", StringComparison.Ordinal))
            .ToList();

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
        CancellationToken cancellationToken = default)
    {
        InvalidatedFiles.Add((userId, cloudAccountUid, fileId));
        return Task.CompletedTask;
    }
}
