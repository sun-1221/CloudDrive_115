using System.Text.Json;

using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Dtos;
using Application.CloudDrive.Jobs;
using Application.Tests.Support;

using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;

namespace Application.Tests.CloudDrive.Jobs;

/// <summary>
/// 115 网盘后台任务编排器测试。
/// </summary>
public sealed class CloudDriveJobOrchestratorTests
{
    private static readonly Guid TestUserId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private const string TestCloudAccountUid = "115001";

    /// <summary>
    /// 验证未采集的上传能力不会创建任务。
    /// </summary>
    [Fact]
    public async Task EnqueueLargeUploadAsync_WhenCapabilityDisabled_ThrowsWithoutSideEffects()
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
        Task Act() => orchestrator.EnqueueLargeUploadAsync(TestUserId, TestCloudAccountUid, input, CancellationToken.None);

        // Assert
        CloudDriveApiNotCollectedException exception = await Assert.ThrowsAsync<CloudDriveApiNotCollectedException>(Act);
        Assert.Equal("cloud_drive_api_not_collected", exception.ErrorCode);
        Assert.Empty(repository.Inserted);
        Assert.Empty(scheduler.Scheduled);
    }

    /// <summary>
    /// 验证未采集的批量能力不会创建任务。
    /// </summary>
    [Theory]
    [InlineData(CloudDriveJobType.BatchCopy)]
    [InlineData(CloudDriveJobType.BatchMove)]
    [InlineData(CloudDriveJobType.BatchDelete)]
    [InlineData(CloudDriveJobType.BatchRecycleBinRestore)]
    [InlineData(CloudDriveJobType.BatchRecycleBinDelete)]
    public async Task EnqueueBatchOperationAsync_WhenCapabilityDisabled_ThrowsWithoutSideEffects(
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
        Task Act() => orchestrator.EnqueueBatchOperationAsync(
            TestUserId,
            TestCloudAccountUid,
            jobType,
            payload,
            CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<CloudDriveApiNotCollectedException>(Act);
        Assert.Empty(repository.Inserted);
        Assert.Empty(scheduler.Scheduled);
    }

    /// <summary>
    /// 验证非批量任务类型不会创建批量任务且抛出异常。
    /// </summary>
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
        await Assert.ThrowsAsync<CloudDriveApiNotCollectedException>(Act);
        Assert.Empty(repository.Inserted);
        Assert.Empty(scheduler.Scheduled);
    }

    /// <summary>
    /// 验证清空回收站不会创建后台任务。
    /// </summary>
    [Fact]
    public async Task EnqueueClearRecycleBinAsync_WhenCapabilityDisabled_ThrowsWithoutSideEffects()
    {
        // Arrange
        var repository = new FakeCloudDriveJobRepository();
        var scheduler = new RecordingJobScheduler();
        CloudDriveJobOrchestrator orchestrator = CreateOrchestrator(repository, scheduler);

        // Act
        Task Act() => orchestrator.EnqueueClearRecycleBinAsync(
            TestUserId,
            TestCloudAccountUid,
            CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<CloudDriveApiNotCollectedException>(Act);
        Assert.Empty(repository.Inserted);
        Assert.Empty(scheduler.Scheduled);
    }

    /// <summary>
    /// 验证主动转码保持禁用，不创建后台任务。
    /// </summary>
    [Fact]
    public async Task EnqueueVideoTranscodeAsync_WhenCapabilityDisabled_ThrowsWithoutSideEffects()
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
        Task Act() => orchestrator.EnqueueVideoTranscodeAsync(TestUserId, TestCloudAccountUid, input, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<CloudDriveApiNotCollectedException>(Act);
        Assert.Empty(repository.Inserted);
        Assert.Empty(scheduler.Scheduled);
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
