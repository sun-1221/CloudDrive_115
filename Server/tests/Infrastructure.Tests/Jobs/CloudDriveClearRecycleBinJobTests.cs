using System.Text.Json;

using Application.CloudDrive.Dtos;

using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;

using Infrastructure.Jobs;

using MediatR;

using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.Tests.Jobs;

public sealed class CloudDriveClearRecycleBinJobTests
{
    private static readonly Guid TestUserId = Guid.Parse("66666666-6666-6666-6666-666666666666");
    private const string TestCloudAccountUid = "115001";

    [Fact]
    public async Task Execute_WhenValidPayload_FailsAsApiNotCollectedWithoutGatewayRequestAsync()
    {
        // Arrange
        CloudDriveJob job = CreateJob(new ClearRecycleBinInput());
        var repository = new CloudDriveJobTestDoubles.FakeCloudDriveJobRepository(job);
        var transactionManager = new CloudDriveJobTestDoubles.RecordingTransactionManager();
        CloudDriveClearRecycleBinJob clearJob = CreateClearJob(
            repository,
            transactionManager,
            new CloudDriveJobTestDoubles.RecordingPublisher());

        // Act
        await clearJob.Execute(CloudDriveJobTestDoubles.CreateExecutionContext(job.Id));

        // Assert
        Assert.Equal(CloudDriveJobStatus.Failed, job.Status);
        Assert.Equal(1, job.Progress);
        Assert.Contains("cloud_drive_api_not_collected", job.ErrorMessage, StringComparison.Ordinal);
        Assert.Null(job.CheckpointData);
        Assert.Equal(2, transactionManager.CommitCount);
        Assert.Equal(0, transactionManager.RollbackCount);
    }

    [Fact]
    public async Task Execute_WhenApiNotCollected_FailsJobAndDoesNotInvalidateCacheAsync()
    {
        // Arrange
        CloudDriveJob job = CreateJob(new ClearRecycleBinInput());
        var repository = new CloudDriveJobTestDoubles.FakeCloudDriveJobRepository(job);
        var transactionManager = new CloudDriveJobTestDoubles.RecordingTransactionManager();
        CloudDriveClearRecycleBinJob clearJob = CreateClearJob(
            repository,
            transactionManager,
            new CloudDriveJobTestDoubles.RecordingPublisher());

        // Act
        await clearJob.Execute(CloudDriveJobTestDoubles.CreateExecutionContext(job.Id));

        // Assert
        Assert.Equal(CloudDriveJobStatus.Failed, job.Status);
        Assert.Equal(1, job.Progress);
        Assert.Contains("cloud_drive_api_not_collected", job.ErrorMessage, StringComparison.Ordinal);
        Assert.Equal(2, transactionManager.CommitCount);
        Assert.Equal(0, transactionManager.RollbackCount);
    }

    private static CloudDriveJob CreateJob(ClearRecycleBinInput input)
    {
        string payloadJson = JsonSerializer.Serialize(input, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        return CloudDriveJob.Create(
            TestUserId,
            TestCloudAccountUid,
            CloudDriveJobType.ClearRecycleBin,
            resourceId: "recycle-bin",
            payloadJson: payloadJson);
    }

    private static CloudDriveClearRecycleBinJob CreateClearJob(
        ICloudDriveJobRepository repository,
        Application.Abstractions.ITransactionManager transactionManager,
        IPublisher publisher)
    {
        return new CloudDriveClearRecycleBinJob(
            repository,
            transactionManager,
            publisher,
            new CloudDriveJobTestDoubles.RecordingJobAuditLogger(),
            new CloudDriveJobTestDoubles.PassThroughSensitiveFieldsFilter(),
            NullLogger<CloudDriveClearRecycleBinJob>.Instance);
    }
}
