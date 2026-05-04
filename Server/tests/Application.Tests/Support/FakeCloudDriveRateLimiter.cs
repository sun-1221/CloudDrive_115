using Application.Abstractions.CloudDrive;

namespace Application.Tests.Support;

internal sealed class FakeCloudDriveRateLimiter : ICloudDriveRateLimiter
{
    public List<(Guid UserId, string CloudAccountUid, string? Category)> Acquisitions { get; } = [];

    public List<(Guid UserId, string CloudAccountUid, string? Category)> ThrottleSignals { get; } = [];

    public List<(Guid UserId, string CloudAccountUid, string? Category)> Resets { get; } = [];

    public Task<IDisposable> AcquireAsync(
        Guid userId,
        string cloudAccountUid,
        string? category = null,
        CancellationToken cancellationToken = default)
    {
        Acquisitions.Add((userId, cloudAccountUid, category));
        return Task.FromResult<IDisposable>(new NoOpDisposable());
    }

    public void RecordThrottleSignal(Guid userId, string cloudAccountUid, string? category = null)
    {
        ThrottleSignals.Add((userId, cloudAccountUid, category));
    }

    public void Reset(Guid userId, string cloudAccountUid, string? category = null)
    {
        Resets.Add((userId, cloudAccountUid, category));
    }

    private sealed class NoOpDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
