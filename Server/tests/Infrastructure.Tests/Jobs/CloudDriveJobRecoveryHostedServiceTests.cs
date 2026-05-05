using System.Text.Json;

using Application.Abstractions;
using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Dtos;

using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;

using Infrastructure.Jobs;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.Tests.Jobs;

public sealed class CloudDriveJobRecoveryHostedServiceTests : IDisposable
{
    private readonly string _tempFilePath = Path.Combine(Path.GetTempPath(), $"cloud-recovery-{Guid.NewGuid():N}.tmp");

    public void Dispose()
    {
        if (File.Exists(_tempFilePath))
        {
            File.Delete(_tempFilePath);
        }
    }

    [Fact]
    public async Task StartAsync_WhenPendingAndRunningJobsRecoverable_SchedulesThemAsync()
    {
        // Arrange
        await File.WriteAllTextAsync(_tempFilePath, "demo");
        CloudDriveJob upload = CreateUploadJob(_tempFilePath);
        CloudDriveJob batch = CloudDriveJob.Create(
            Guid.NewGuid(),
            "115001",
            CloudDriveJobType.BatchDelete,
            payloadJson: """{"cloudAccountUid":"115001","fileIds":["file-1"]}""");
        batch.Start();

        var repository = new RecoveryJobRepository([upload, batch]);
        var scheduler = new RecordingJobScheduler();
        CloudDriveJobRecoveryHostedService service = CreateService(repository, scheduler);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert
        Assert.Equal(
            [(upload.Id, CloudDriveJobType.Upload), (batch.Id, CloudDriveJobType.BatchDelete)],
            scheduler.Scheduled);
        Assert.Equal(CloudDriveJobStatus.Pending, upload.Status);
        Assert.Equal(CloudDriveJobStatus.Running, batch.Status);
    }

    [Fact]
    public async Task StartAsync_WhenJobsNotRecoverable_MarksFailedAsync()
    {
        // Arrange
        CloudDriveJob missingPayload = CloudDriveJob.Create(
            Guid.NewGuid(),
            "115001",
            CloudDriveJobType.ClearRecycleBin);
        CloudDriveJob missingTempFile = CreateUploadJob(Path.Combine(Path.GetTempPath(), "missing-upload.tmp"));
        var repository = new RecoveryJobRepository([missingPayload, missingTempFile]);
        var scheduler = new RecordingJobScheduler();
        var transactionManager = new CloudDriveJobTestDoubles.RecordingTransactionManager();
        CloudDriveJobRecoveryHostedService service = CreateService(repository, scheduler, transactionManager);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert
        Assert.Empty(scheduler.Scheduled);
        Assert.Equal(CloudDriveJobStatus.Failed, missingPayload.Status);
        Assert.Equal(CloudDriveJobStatus.Failed, missingTempFile.Status);
        Assert.Contains("缺少 PayloadJson", missingPayload.ErrorMessage, StringComparison.Ordinal);
        Assert.Contains("本地临时文件不可用", missingTempFile.ErrorMessage, StringComparison.Ordinal);
        Assert.Equal(2, transactionManager.CommitCount);
    }

    private CloudDriveJob CreateUploadJob(string tempFilePath)
    {
        string payloadJson = JsonSerializer.Serialize(
            new UploadFileInput
            {
                DirectoryId = "0",
                FileName = "demo.txt",
                FileSize = 4,
                FileSha1 = "SHA1",
                TempFilePath = tempFilePath
            },
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        return CloudDriveJob.Create(
            Guid.NewGuid(),
            "115001",
            CloudDriveJobType.Upload,
            payloadJson: payloadJson);
    }

    private static CloudDriveJobRecoveryHostedService CreateService(
        ICloudDriveJobRepository repository,
        ICloudDriveJobScheduler scheduler,
        ITransactionManager? transactionManager = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(repository);
        services.AddSingleton(scheduler);
        services.AddSingleton(transactionManager ?? new CloudDriveJobTestDoubles.RecordingTransactionManager());
        services.AddSingleton<ICloudDriveJobAuditLogger, CloudDriveJobTestDoubles.RecordingJobAuditLogger>();
        ServiceProvider provider = services.BuildServiceProvider();

        return new CloudDriveJobRecoveryHostedService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<CloudDriveJobRecoveryHostedService>.Instance);
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

    private sealed class RecoveryJobRepository(IEnumerable<CloudDriveJob> jobs) : ICloudDriveJobRepository
    {
        private readonly List<CloudDriveJob> _jobs = jobs.ToList();

        public List<CloudDriveJob> Updated { get; } = [];

        public Task<CloudDriveJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_jobs.FirstOrDefault(job => job.Id == id));
        }

        public Task<IReadOnlyList<CloudDriveJob>> GetListAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<CloudDriveJob>>(_jobs);
        }

        public Task<int> InsertAsync(CloudDriveJob entity, CancellationToken cancellationToken = default)
        {
            _jobs.Add(entity);
            return Task.FromResult(1);
        }

        public Task<int> UpdateAsync(CloudDriveJob entity, CancellationToken cancellationToken = default)
        {
            Updated.Add(entity);
            return Task.FromResult(1);
        }

        public Task<int> DeleteAsync(CloudDriveJob entity, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_jobs.Remove(entity) ? 1 : 0);
        }

        public Task<IReadOnlyList<CloudDriveJob>> GetByUserAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<CloudDriveJob> result = _jobs.Where(job => job.UserId == userId).ToList();
            return Task.FromResult(result);
        }

        public Task<CloudDriveJob?> GetByIdForUserAsync(
            Guid userId,
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_jobs.FirstOrDefault(job => job.UserId == userId && job.Id == id));
        }

        public Task<IReadOnlyList<CloudDriveJob>> GetByStatusesAsync(
            IEnumerable<CloudDriveJobStatus> statuses,
            CancellationToken cancellationToken = default)
        {
            HashSet<CloudDriveJobStatus> statusSet = statuses.ToHashSet();
            IReadOnlyList<CloudDriveJob> result = _jobs.Where(job => statusSet.Contains(job.Status)).ToList();
            return Task.FromResult(result);
        }
    }
}
