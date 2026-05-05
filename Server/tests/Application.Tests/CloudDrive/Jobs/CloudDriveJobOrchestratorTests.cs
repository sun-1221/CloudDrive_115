using System.Text.Json;

using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Dtos;
using Application.CloudDrive.Jobs;
using Application.Tests.Support;

using Domain.Entities;
using Domain.Enums;

namespace Application.Tests.CloudDrive.Jobs;

public sealed class CloudDriveJobOrchestratorTests
{
    private static readonly Guid TestUserId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private const string TestCloudAccountUid = "115001";

    [Fact]
    public async Task EnqueueLargeUploadAsync_WhenValid_CreatesUploadJobWithSerializedPayloadAndSchedulesIt()
    {
        // Arrange
        var repository = new FakeCloudDriveJobRepository();
        var scheduler = new RecordingJobScheduler();
        CloudDriveJobOrchestrator orchestrator = CreateOrchestrator(repository, scheduler);
        var input = new UploadFileInput
        {
            DirectoryId = "dir-001",
            FileName = "video.mp4",
            FileSize = 1024,
            FileSha1 = "ABCDEF",
            TempFilePath = @"D:\temp\upload.tmp",
            PickCode = "pick-001",
            ForceAsync = true
        };

        // Act
        Guid jobId = await orchestrator.EnqueueLargeUploadAsync(TestUserId, TestCloudAccountUid, input, CancellationToken.None);

        // Assert
        CloudDriveJob job = Assert.Single(repository.Inserted);
        Assert.Equal(job.Id, jobId);
        Assert.Equal(TestUserId, job.UserId);
        Assert.Equal(TestCloudAccountUid, job.CloudAccountUid);
        Assert.Equal(CloudDriveJobType.Upload, job.JobType);
        Assert.Equal("dir-001", job.ResourceId);
        Assert.Equal(CloudDriveJobStatus.Pending, job.Status);
        Assert.Equal([(job.Id, CloudDriveJobType.Upload)], scheduler.Scheduled);

        using JsonDocument payload = JsonDocument.Parse(job.PayloadJson!);
        Assert.Equal("video.mp4", payload.RootElement.GetProperty("fileName").GetString());
        Assert.Equal(@"D:\temp\upload.tmp", payload.RootElement.GetProperty("tempFilePath").GetString());
        Assert.Equal("pick-001", payload.RootElement.GetProperty("pickCode").GetString());
    }

    [Theory]
    [InlineData(CloudDriveJobType.BatchCopy)]
    [InlineData(CloudDriveJobType.BatchMove)]
    [InlineData(CloudDriveJobType.BatchDelete)]
    [InlineData(CloudDriveJobType.BatchRecycleBinRestore)]
    [InlineData(CloudDriveJobType.BatchRecycleBinDelete)]
    public async Task EnqueueBatchOperationAsync_WhenValidBatchType_CreatesJobAndSchedulesIt(
        CloudDriveJobType jobType)
    {
        // Arrange
        var repository = new FakeCloudDriveJobRepository();
        var scheduler = new RecordingJobScheduler();
        CloudDriveJobOrchestrator orchestrator = CreateOrchestrator(repository, scheduler);
        var payload = new
        {
            FileIds = new[] { "file-001", "file-002" },
            TargetDirectoryId = "target-001"
        };

        // Act
        Guid jobId = await orchestrator.EnqueueBatchOperationAsync(
            TestUserId,
            TestCloudAccountUid,
            jobType,
            payload,
            CancellationToken.None);

        // Assert
        CloudDriveJob job = Assert.Single(repository.Inserted);
        Assert.Equal(job.Id, jobId);
        Assert.Equal(jobType, job.JobType);
        Assert.Equal(TestCloudAccountUid, job.CloudAccountUid);
        Assert.Null(job.ResourceId);
        Assert.Equal([(job.Id, jobType)], scheduler.Scheduled);

        using JsonDocument payloadJson = JsonDocument.Parse(job.PayloadJson!);
        Assert.Equal("target-001", payloadJson.RootElement.GetProperty("targetDirectoryId").GetString());
        Assert.Equal("file-001", payloadJson.RootElement.GetProperty("fileIds")[0].GetString());
    }

    [Fact]
    public async Task EnqueueBatchOperationAsync_WhenNonBatchType_ThrowsAndDoesNotSchedule()
    {
        // Arrange
        var repository = new FakeCloudDriveJobRepository();
        var scheduler = new RecordingJobScheduler();
        CloudDriveJobOrchestrator orchestrator = CreateOrchestrator(repository, scheduler);

        // Act
        Task Act() => orchestrator.EnqueueBatchOperationAsync(
            TestUserId,
            TestCloudAccountUid,
            CloudDriveJobType.ClearRecycleBin,
            new { },
            CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(Act);
        Assert.Empty(repository.Inserted);
        Assert.Empty(scheduler.Scheduled);
    }

    [Fact]
    public async Task EnqueueClearRecycleBinAsync_WhenValid_CreatesClearRecycleBinJob()
    {
        // Arrange
        var repository = new FakeCloudDriveJobRepository();
        var scheduler = new RecordingJobScheduler();
        CloudDriveJobOrchestrator orchestrator = CreateOrchestrator(repository, scheduler);

        // Act
        Guid jobId = await orchestrator.EnqueueClearRecycleBinAsync(
            TestUserId,
            TestCloudAccountUid,
            CancellationToken.None);

        // Assert
        CloudDriveJob job = Assert.Single(repository.Inserted);
        Assert.Equal(job.Id, jobId);
        Assert.Equal(CloudDriveJobType.ClearRecycleBin, job.JobType);
        Assert.Equal("recycle-bin", job.ResourceId);
        Assert.Equal([(job.Id, CloudDriveJobType.ClearRecycleBin)], scheduler.Scheduled);

        using JsonDocument payload = JsonDocument.Parse(job.PayloadJson!);
        Assert.False(payload.RootElement.TryGetProperty("cloudAccountUid", out _));
    }

    [Fact]
    public async Task EnqueueVideoTranscodeAsync_WhenFileIdPresent_UsesFileIdAsResourceId()
    {
        // Arrange
        var repository = new FakeCloudDriveJobRepository();
        var scheduler = new RecordingJobScheduler();
        CloudDriveJobOrchestrator orchestrator = CreateOrchestrator(repository, scheduler);
        var input = new SubmitTranscodeInput
        {
            FileId = "file-001",
            PickCode = "pick-001",
            Op = "vip_push"
        };

        // Act
        Guid jobId = await orchestrator.EnqueueVideoTranscodeAsync(TestUserId, TestCloudAccountUid, input, CancellationToken.None);

        // Assert
        CloudDriveJob job = Assert.Single(repository.Inserted);
        Assert.Equal(job.Id, jobId);
        Assert.Equal(CloudDriveJobType.Transcode, job.JobType);
        Assert.Equal("file-001", job.ResourceId);
        Assert.Equal([(job.Id, CloudDriveJobType.Transcode)], scheduler.Scheduled);

        using JsonDocument payload = JsonDocument.Parse(job.PayloadJson!);
        Assert.Equal("pick-001", payload.RootElement.GetProperty("pickCode").GetString());
        Assert.Equal("vip_push", payload.RootElement.GetProperty("op").GetString());
    }

    private static CloudDriveJobOrchestrator CreateOrchestrator(
        FakeCloudDriveJobRepository repository,
        RecordingJobScheduler scheduler)
    {
        return new CloudDriveJobOrchestrator(
            repository,
            scheduler,
            new RecordingJobAuditLogger(),
            new PassThroughSensitiveFieldsFilter(),
            new FakeLogger<CloudDriveJobOrchestrator>());
    }

    private sealed class RecordingJobScheduler : ICloudDriveJobScheduler
    {
        public List<(Guid JobId, CloudDriveJobType JobType)> Scheduled { get; } = [];

        public Task ScheduleAsync(
            Guid jobId,
            CloudDriveJobType jobType,
            CancellationToken cancellationToken = default)
        {
            Scheduled.Add((jobId, jobType));
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingJobAuditLogger : ICloudDriveJobAuditLogger
    {
        public void LogCreated(Domain.Entities.CloudDriveJob job) { }

        public void LogState(Domain.Entities.CloudDriveJob job, string action) { }

        public void LogError(Domain.Entities.CloudDriveJob job, Exception exception) { }
    }

    private sealed class PassThroughSensitiveFieldsFilter : ICloudDriveSensitiveFieldsFilter
    {
        public string Redact(string? value) => value ?? string.Empty;

        public IReadOnlyDictionary<string, string?> RedactFields(IReadOnlyDictionary<string, string?> fields) => fields;
    }
}
