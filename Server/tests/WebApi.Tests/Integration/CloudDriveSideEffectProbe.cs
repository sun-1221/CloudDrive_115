using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Dtos;
using Domain.Enums;

namespace WebApi.Tests.Integration;

internal sealed class CloudDriveSideEffectProbe : ICloudDriveWebApiGateway, ICloudDriveJobOrchestrator, ICloudDriveMetadataCache
{
    public int GatewayCalls { get; private set; }
    public int JobWrites { get; private set; }
    public int CacheInvalidations { get; private set; }

    public Task<T> SendWebApiRequestAsync<T>(Guid userId, string cloudAccountUid, HttpMethod method, string endpoint, CloudDriveApiContext context, CancellationToken cancellationToken = default)
    {
        GatewayCalls++;
        return Task.FromResult(default(T)!);
    }

    public Task<T> SendUserCenterRequestAsync<T>(Guid userId, string cloudAccountUid, HttpMethod method, string endpoint, CloudDriveApiContext context, CancellationToken cancellationToken = default)
    {
        GatewayCalls++;
        return Task.FromResult(default(T)!);
    }

    public Task<T> SendOfflineApiRequestAsync<T>(Guid userId, string cloudAccountUid, HttpMethod method, string endpoint, CloudDriveApiContext context, CancellationToken cancellationToken = default)
    {
        GatewayCalls++;
        return Task.FromResult(default(T)!);
    }

    public Task<T> SendUploadInitRequestAsync<T>(Guid userId, string cloudAccountUid, CloudDriveApiContext context, CancellationToken cancellationToken = default)
    {
        GatewayCalls++;
        return Task.FromResult(default(T)!);
    }

    public Task<T> UploadFileAsync<T>(Guid userId, string cloudAccountUid, string uploadUrl, Stream fileStream, string fileName, IDictionary<string, string>? formFields = null, CancellationToken cancellationToken = default)
    {
        GatewayCalls++;
        return Task.FromResult(default(T)!);
    }

    public Task<Guid> EnqueueLargeUploadAsync(Guid userId, string cloudAccountUid, UploadFileInput input, CancellationToken cancellationToken = default)
    {
        JobWrites++;
        return Task.FromResult(Guid.NewGuid());
    }

    public Task<Guid> EnqueueBatchOperationAsync(Guid userId, string cloudAccountUid, CloudDriveJobType jobType, object payload, CancellationToken cancellationToken = default)
    {
        JobWrites++;
        return Task.FromResult(Guid.NewGuid());
    }

    public Task<Guid> EnqueueClearRecycleBinAsync(Guid userId, string cloudAccountUid, CancellationToken cancellationToken = default)
    {
        JobWrites++;
        return Task.FromResult(Guid.NewGuid());
    }

    public Task<Guid> EnqueueVideoTranscodeAsync(Guid userId, string cloudAccountUid, SubmitTranscodeInput input, CancellationToken cancellationToken = default)
    {
        JobWrites++;
        return Task.FromResult(Guid.NewGuid());
    }

    public async Task<T?> GetOrSetAsync<T>(string cacheKey, Func<CancellationToken, Task<T>> factory, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        return await factory(cancellationToken);
    }

    public Task InvalidateDirectoryAsync(Guid userId, string cloudAccountUid, string directoryId, CancellationToken cancellationToken = default)
    {
        CacheInvalidations++;
        return Task.CompletedTask;
    }

    public Task InvalidateFileAsync(Guid userId, string cloudAccountUid, string fileId, CancellationToken cancellationToken = default)
    {
        CacheInvalidations++;
        return Task.CompletedTask;
    }
}
