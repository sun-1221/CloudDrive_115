using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;

namespace Application.Tests.Support;

/// <summary>
/// 测试用后台任务仓储，基于内存字典。
/// </summary>
internal sealed class FakeCloudDriveJobRepository : ICloudDriveJobRepository
{
    private readonly Dictionary<Guid, CloudDriveJob> _store = [];

    public FakeCloudDriveJobRepository()
    {
    }

    public FakeCloudDriveJobRepository(IEnumerable<CloudDriveJob> jobs)
    {
        ArgumentNullException.ThrowIfNull(jobs);

        foreach (CloudDriveJob job in jobs)
        {
            _store[job.Id] = job;
        }
    }

    public List<CloudDriveJob> Inserted { get; } = [];

    public List<CloudDriveJob> Updated { get; } = [];

    public Task<CloudDriveJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(id, out CloudDriveJob? job);
        return Task.FromResult(job);
    }

    public Task<IReadOnlyList<CloudDriveJob>> GetListAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<CloudDriveJob>>(_store.Values.ToList().AsReadOnly());
    }

    public Task<int> InsertAsync(CloudDriveJob entity, CancellationToken cancellationToken = default)
    {
        _store[entity.Id] = entity;
        Inserted.Add(entity);
        return Task.FromResult(1);
    }

    public Task<int> UpdateAsync(CloudDriveJob entity, CancellationToken cancellationToken = default)
    {
        _store[entity.Id] = entity;
        Updated.Add(entity);
        return Task.FromResult(1);
    }

    public Task<int> DeleteAsync(CloudDriveJob entity, CancellationToken cancellationToken = default)
    {
        _store.Remove(entity.Id);
        return Task.FromResult(1);
    }

    public Task<IReadOnlyList<CloudDriveJob>> GetByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CloudDriveJob> result = _store.Values
            .Where(job => job.UserId == userId)
            .ToList()
            .AsReadOnly();

        return Task.FromResult(result);
    }

    public Task<CloudDriveJob?> GetByIdForUserAsync(
        Guid userId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        CloudDriveJob? job = _store.Values.FirstOrDefault(item => item.UserId == userId && item.Id == id);
        return Task.FromResult(job);
    }

    public Task<IReadOnlyList<CloudDriveJob>> GetByStatusesAsync(
        IEnumerable<CloudDriveJobStatus> statuses,
        CancellationToken cancellationToken = default)
    {
        HashSet<CloudDriveJobStatus> statusSet = statuses.ToHashSet();
        IReadOnlyList<CloudDriveJob> result = _store.Values
            .Where(job => statusSet.Contains(job.Status))
            .ToList()
            .AsReadOnly();

        return Task.FromResult(result);
    }
}
