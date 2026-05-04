using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Dtos;

using Domain.Enums;

namespace Application.Tests.Support;

internal sealed class FakeCloudDriveJobOrchestrator : ICloudDriveJobOrchestrator
{
    public Guid NextJobId { get; set; } = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    public List<(Guid UserId, UploadFileInput Input)> Uploads { get; } = [];

    public List<(Guid UserId, string CloudAccountUid, CloudDriveJobType JobType, object Payload)> BatchOperations { get; } = [];

    public List<(Guid UserId, string CloudAccountUid)> ClearRecycleBins { get; } = [];

    public List<(Guid UserId, SubmitTranscodeInput Input)> Transcodes { get; } = [];

    public Task<Guid> EnqueueLargeUploadAsync(
        Guid userId,
        UploadFileInput input,
        CancellationToken cancellationToken = default)
    {
        Uploads.Add((userId, input));
        return Task.FromResult(NextJobId);
    }

    public Task<Guid> EnqueueBatchOperationAsync(
        Guid userId,
        string cloudAccountUid,
        CloudDriveJobType jobType,
        object payload,
        CancellationToken cancellationToken = default)
    {
        BatchOperations.Add((userId, cloudAccountUid, jobType, payload));
        return Task.FromResult(NextJobId);
    }

    public Task<Guid> EnqueueClearRecycleBinAsync(
        Guid userId,
        string cloudAccountUid,
        CancellationToken cancellationToken = default)
    {
        ClearRecycleBins.Add((userId, cloudAccountUid));
        return Task.FromResult(NextJobId);
    }

    public Task<Guid> EnqueueVideoTranscodeAsync(
        Guid userId,
        SubmitTranscodeInput input,
        CancellationToken cancellationToken = default)
    {
        Transcodes.Add((userId, input));
        return Task.FromResult(NextJobId);
    }
}
