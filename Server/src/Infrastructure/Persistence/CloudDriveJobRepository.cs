using Application.Abstractions;

using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;

using SqlSugar;

namespace Infrastructure.Persistence;

/// <summary>
/// 115 云盘后台任务仓储的 SqlSugar 实现。
/// </summary>
internal sealed class CloudDriveJobRepository(
    ISqlSugarClient db,
    IDomainEventSource domainEventSource) : ICloudDriveJobRepository
{
    /// <summary>
    /// 根据任务标识查询后台任务。
    /// </summary>
    public async Task<CloudDriveJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await db.Queryable<CloudDriveJob>().InSingleAsync(id);
    }

    /// <summary>
    /// 查询所有后台任务。
    /// </summary>
    public async Task<IReadOnlyList<CloudDriveJob>> GetListAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await db.Queryable<CloudDriveJob>()
            .OrderBy(job => job.CreationTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 插入后台任务并追踪领域事件。
    /// </summary>
    public async Task<int> InsertAsync(CloudDriveJob entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        int rows = await db.Insertable(entity).ExecuteCommandAsync(cancellationToken);
        if (rows > 0)
        {
            // 只有数据库实际写入成功后才追踪领域事件，避免事务回滚前发布不存在的任务变更。
            domainEventSource.Track(entity);
        }

        return rows;
    }

    /// <summary>
    /// 更新后台任务并追踪领域事件。
    /// </summary>
    public async Task<int> UpdateAsync(CloudDriveJob entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        int rows = await db.Updateable(entity).ExecuteCommandAsync(cancellationToken);
        if (rows > 0)
        {
            // 更新成功后把聚合交给 Pipeline 统一分发领域事件，仓储本身不直接发布事件。
            domainEventSource.Track(entity);
        }

        return rows;
    }

    /// <summary>
    /// 删除后台任务并追踪领域事件。
    /// </summary>
    public async Task<int> DeleteAsync(CloudDriveJob entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        int rows = await db.Deleteable(entity).ExecuteCommandAsync(cancellationToken);
        if (rows > 0)
        {
            // 删除也可能携带领域事件，仍需在事务提交前纳入统一分发队列。
            domainEventSource.Track(entity);
        }

        return rows;
    }

    /// <summary>
    /// 查询指定用户的后台任务。
    /// </summary>
    public async Task<IReadOnlyList<CloudDriveJob>> GetByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await db.Queryable<CloudDriveJob>()
            .Where(job => job.UserId == userId)
            .OrderBy(job => job.CreationTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 在用户范围内根据任务标识查询后台任务。
    /// </summary>
    public async Task<CloudDriveJob?> GetByIdForUserAsync(
        Guid userId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await db.Queryable<CloudDriveJob>()
            .Where(job => job.UserId == userId && job.Id == id)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 查询指定状态集合下可调度或可恢复的后台任务。
    /// </summary>
    public async Task<IReadOnlyList<CloudDriveJob>> GetByStatusesAsync(
        IEnumerable<CloudDriveJobStatus> statuses,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(statuses);

        int[] statusArray = statuses
            .Distinct()
            .Select(status => (int)status)
            .ToArray();

        // 空状态集合直接返回空结果，避免生成无意义的 IN () SQL。
        if (statusArray.Length == 0)
        {
            return [];
        }

        // SqlSugar 对枚举 Contains 的跨数据库翻译不稳定，显式转 int 保持 SQLite/生产库一致。
        return await db.Queryable<CloudDriveJob>()
            .Where(job => statusArray.Contains(SqlFunc.ToInt32(job.Status)))
            .OrderBy(job => job.CreationTime, OrderByType.Asc)
            .ToListAsync(cancellationToken);
    }
}
