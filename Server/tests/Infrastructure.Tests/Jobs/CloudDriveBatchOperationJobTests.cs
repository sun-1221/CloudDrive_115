using System.Text.Json;

using Application.CloudDrive.Dtos;

using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;

using Infrastructure.Jobs;

using MediatR;

using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.Tests.Jobs;

public sealed class CloudDriveBatchOperationJobTests
{
    private static readonly Guid TestUserId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private const string TestCloudAccountUid = "115001";

    [Theory]
    [MemberData(nameof(BatchOperationCases))]
    public async Task Execute_WhenValidBatchPayload_FailsAsApiNotCollectedWithoutGatewayRequestAsync(
        BatchOperationCase testCase)
    {
        // Arrange
        CloudDriveJob job = CreateJob(testCase.JobType, testCase.Payload);
        var repository = new CloudDriveJobTestDoubles.FakeCloudDriveJobRepository(job);
        var metadataCache = new CloudDriveJobTestDoubles.RecordingMetadataCache();
        var transactionManager = new CloudDriveJobTestDoubles.RecordingTransactionManager();
        var publisher = new CloudDriveJobTestDoubles.RecordingPublisher();
        CloudDriveBatchOperationJob batchJob = CreateBatchJob(
            repository,
            transactionManager,
            publisher);

        // Act
        await batchJob.Execute(CloudDriveJobTestDoubles.CreateExecutionContext(job.Id));

        // Assert
        Assert.Equal(CloudDriveJobStatus.Failed, job.Status);
        Assert.Equal(1, job.Progress);
        Assert.Contains("cloud_drive_api_not_collected", job.ErrorMessage, StringComparison.Ordinal);
        Assert.Null(job.CheckpointData);
        Assert.Equal(2, transactionManager.CommitCount);
        Assert.Equal(0, transactionManager.RollbackCount);
        Assert.Empty(metadataCache.InvalidatedDirectories);
    }

    [Fact]
    public async Task Execute_WhenApiNotCollected_FailsJobAndDoesNotInvalidateCacheAsync()
    {
        // Arrange
        CloudDriveJob job = CreateJob(
            CloudDriveJobType.BatchDelete,
            new DeleteFilesInput
            {
                FileIds = ["file-1", "file-2"]
            });
        var repository = new CloudDriveJobTestDoubles.FakeCloudDriveJobRepository(job);
        var metadataCache = new CloudDriveJobTestDoubles.RecordingMetadataCache();
        var transactionManager = new CloudDriveJobTestDoubles.RecordingTransactionManager();
        CloudDriveBatchOperationJob batchJob = CreateBatchJob(
            repository,
            transactionManager,
            new CloudDriveJobTestDoubles.RecordingPublisher());

        // Act
        await batchJob.Execute(CloudDriveJobTestDoubles.CreateExecutionContext(job.Id));

        // Assert
        Assert.Equal(CloudDriveJobStatus.Failed, job.Status);
        Assert.Equal(1, job.Progress);
        Assert.Contains("cloud_drive_api_not_collected", job.ErrorMessage, StringComparison.Ordinal);
        Assert.Empty(metadataCache.InvalidatedDirectories);
        Assert.Equal(2, transactionManager.CommitCount);
        Assert.Equal(0, transactionManager.RollbackCount);
    }

    public static IEnumerable<object[]> BatchOperationCases()
    {
        yield return
        [
            new BatchOperationCase(
                CloudDriveJobType.BatchCopy,
                new CopyFilesInput
                {
                    FileIds = ["file-1", "file-2"],
                    TargetDirectoryId = "target-1"
                },
                "BatchCopy")
        ];

        yield return
        [
            new BatchOperationCase(
                CloudDriveJobType.BatchMove,
                new MoveFilesInput
                {
                    FileIds = ["file-1", "file-2"],
                    TargetDirectoryId = "target-1"
                },
                "BatchMove")
        ];

        yield return
        [
            new BatchOperationCase(
                CloudDriveJobType.BatchDelete,
                new DeleteFilesInput
                {
                    FileIds = ["file-1", "file-2"]
                },
                "BatchDelete")
        ];

        yield return
        [
            new BatchOperationCase(
                CloudDriveJobType.BatchRecycleBinRestore,
                new RecycleBinOperationInput
                {
                    RecycleBinItemIds = ["recycle-1", "recycle-2"]
                },
                "BatchRecycleBinRestore")
        ];

        yield return
        [
            new BatchOperationCase(
                CloudDriveJobType.BatchRecycleBinDelete,
                new RecycleBinOperationInput
                {
                    RecycleBinItemIds = ["recycle-1", "recycle-2"]
                },
                "BatchRecycleBinDelete")
        ];
    }

    private static CloudDriveJob CreateJob(CloudDriveJobType jobType, object payload)
    {
        string payloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        return CloudDriveJob.Create(
            TestUserId,
            TestCloudAccountUid,
            jobType,
            payloadJson: payloadJson);
    }

    private static CloudDriveBatchOperationJob CreateBatchJob(
        ICloudDriveJobRepository repository,
        Application.Abstractions.ITransactionManager transactionManager,
        IPublisher publisher)
    {
        return new CloudDriveBatchOperationJob(
            repository,
            transactionManager,
            publisher,
            new CloudDriveJobTestDoubles.RecordingJobAuditLogger(),
            new CloudDriveJobTestDoubles.PassThroughSensitiveFieldsFilter(),
            NullLogger<CloudDriveBatchOperationJob>.Instance);
    }

    public sealed record BatchOperationCase(
        CloudDriveJobType JobType,
        object Payload,
        string Label)
    {
        public override string ToString() => JobType.ToString();
    }
}
