using Application.Abstractions;
using Application.Abstractions.CloudDrive;

using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;

using MediatR;

using Quartz;

namespace Infrastructure.Tests.Jobs;

internal static class CloudDriveJobTestDoubles
{
    public static IJobExecutionContext CreateExecutionContext(Guid jobId)
    {
        return new FakeJobExecutionContext(jobId);
    }

    internal sealed class FakeJobExecutionContext(Guid jobId) : IJobExecutionContext
    {
        private readonly Dictionary<object, object> _items = [];

        public IScheduler Scheduler => throw new NotSupportedException();

        public ITrigger Trigger => throw new NotSupportedException();

        public ICalendar? Calendar => null;

        public bool Recovering => false;

        public TriggerKey RecoveringTriggerKey => new("recovering", "cloud-drive");

        public int RefireCount => 0;

        public JobDataMap MergedJobDataMap { get; } = new() { ["JobId"] = jobId.ToString("D") };

        public IJobDetail JobDetail => throw new NotSupportedException();

        public IJob JobInstance => throw new NotSupportedException();

        public DateTimeOffset FireTimeUtc => DateTimeOffset.UtcNow;

        public DateTimeOffset? ScheduledFireTimeUtc => FireTimeUtc;

        public DateTimeOffset? PreviousFireTimeUtc => null;

        public DateTimeOffset? NextFireTimeUtc => null;

        public string FireInstanceId => jobId.ToString("N");

        public object? Result { get; set; }

        public TimeSpan JobRunTime => TimeSpan.Zero;

        public CancellationToken CancellationToken => CancellationToken.None;

        public void Put(object key, object objectValue)
        {
            _items[key] = objectValue;
        }

        public object? Get(object key)
        {
            _items.TryGetValue(key, out object? value);
            return value;
        }
    }

    internal sealed class FakeCloudDriveJobRepository(CloudDriveJob job) : ICloudDriveJobRepository
    {
        public List<CloudDriveJob> Updates { get; } = [];

        public Task<CloudDriveJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(id == job.Id ? job : null);
        }

        public Task<IReadOnlyList<CloudDriveJob>> GetListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CloudDriveJob>>(new[] { job });

        public Task<int> InsertAsync(CloudDriveJob entity, CancellationToken cancellationToken = default) =>
            Task.FromResult(1);

        public Task<int> UpdateAsync(CloudDriveJob entity, CancellationToken cancellationToken = default)
        {
            Updates.Add(entity);
            return Task.FromResult(1);
        }

        public Task<int> DeleteAsync(CloudDriveJob entity, CancellationToken cancellationToken = default) =>
            Task.FromResult(1);

        public Task<IReadOnlyList<CloudDriveJob>> GetByUserAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CloudDriveJob>>(new[] { job }.Where(item => item.UserId == userId).ToList());

        public Task<CloudDriveJob?> GetByIdForUserAsync(
            Guid userId,
            Guid id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(job.UserId == userId && job.Id == id ? job : null);

        public Task<IReadOnlyList<CloudDriveJob>> GetByStatusesAsync(
            IEnumerable<CloudDriveJobStatus> statuses,
            CancellationToken cancellationToken = default)
        {
            HashSet<CloudDriveJobStatus> statusSet = statuses.ToHashSet();
            IReadOnlyList<CloudDriveJob> result = statusSet.Contains(job.Status) ? [job] : [];
            return Task.FromResult(result);
        }
    }

    internal sealed class RecordingMetadataCache : ICloudDriveMetadataCache
    {
        public List<(Guid UserId, string CloudAccountUid, string DirectoryId)> InvalidatedDirectories { get; } = [];

        public async Task<T?> GetOrSetAsync<T>(
            string cacheKey,
            Func<CancellationToken, Task<T>> factory,
            TimeSpan? ttl = null,
            CancellationToken cancellationToken = default)
        {
            return await factory(cancellationToken);
        }

        public Task InvalidateDirectoryAsync(
            Guid userId,
            string cloudAccountUid,
            string directoryId,
            CancellationToken cancellationToken = default)
        {
            InvalidatedDirectories.Add((userId, cloudAccountUid, directoryId));
            return Task.CompletedTask;
        }

        public Task InvalidateFileAsync(
            Guid userId,
            string cloudAccountUid,
            string fileId,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    internal sealed class RecordingTransactionManager : ITransactionManager
    {
        public int CommitCount { get; private set; }

        public int RollbackCount { get; private set; }

        public Task BeginAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            CommitCount++;
            return Task.CompletedTask;
        }

        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            RollbackCount++;
            return Task.CompletedTask;
        }
    }

    internal sealed class RecordingPublisher : IPublisher
    {
        public List<object> PublishedNotifications { get; } = [];

        public Task Publish(object notification, CancellationToken cancellationToken = default)
        {
            PublishedNotifications.Add(notification);
            return Task.CompletedTask;
        }

        public Task Publish<TNotification>(
            TNotification notification,
            CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            PublishedNotifications.Add(notification);
            return Task.CompletedTask;
        }
    }

    internal sealed class RecordingJobAuditLogger : ICloudDriveJobAuditLogger
    {
        public List<(Guid JobId, string Action)> States { get; } = [];

        public List<Guid> Created { get; } = [];

        public List<(Guid JobId, string Message)> Errors { get; } = [];

        public void LogCreated(CloudDriveJob job)
        {
            Created.Add(job.Id);
        }

        public void LogState(CloudDriveJob job, string action)
        {
            States.Add((job.Id, action));
        }

        public void LogError(CloudDriveJob job, Exception exception)
        {
            Errors.Add((job.Id, exception.Message));
        }
    }
}
