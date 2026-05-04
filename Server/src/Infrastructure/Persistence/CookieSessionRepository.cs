using Application.Abstractions;

using Domain.Entities;
using Domain.Repositories;

using SqlSugar;

namespace Infrastructure.Persistence;

/// <summary>
/// 115 Cookie 会话仓储的 SqlSugar 实现。
/// </summary>
internal sealed class CookieSessionRepository(
    ISqlSugarClient db,
    IDomainEventSource domainEventSource) : ICookieSessionRepository
{
    /// <summary>
    /// 根据会话标识查询 Cookie 会话。
    /// </summary>
    public async Task<CookieSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await db.Queryable<CookieSession>().InSingleAsync(id);
    }

    /// <summary>
    /// 查询全部 Cookie 会话。
    /// </summary>
    public async Task<IReadOnlyList<CookieSession>> GetListAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await db.Queryable<CookieSession>()
            .OrderBy(session => session.CreationTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 插入 Cookie 会话并追踪领域事件。
    /// </summary>
    public async Task<int> InsertAsync(CookieSession entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        int rows = await db.Insertable(entity).ExecuteCommandAsync(cancellationToken);
        if (rows > 0)
        {
            // 会话创建成功后才追踪领域事件，避免后续通知引用未持久化的会话。
            domainEventSource.Track(entity);
        }

        return rows;
    }

    /// <summary>
    /// 更新 Cookie 会话并追踪领域事件。
    /// </summary>
    public async Task<int> UpdateAsync(CookieSession entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        int rows = await db.Updateable(entity).ExecuteCommandAsync(cancellationToken);
        if (rows > 0)
        {
            // Cookie 失效、冷却和密文轮换都可能产生领域事件，由 Pipeline 在事务边界统一分发。
            domainEventSource.Track(entity);
        }

        return rows;
    }

    /// <summary>
    /// 删除 Cookie 会话并追踪领域事件。
    /// </summary>
    public async Task<int> DeleteAsync(CookieSession entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        int rows = await db.Deleteable(entity).ExecuteCommandAsync(cancellationToken);
        if (rows > 0)
        {
            // 删除成功后再追踪聚合，避免数据库未变更时产生错误的删除事件。
            domainEventSource.Track(entity);
        }

        return rows;
    }

    /// <summary>
    /// 根据用户与 115 账号 UID 查询唯一 Cookie 会话。
    /// </summary>
    public async Task<CookieSession?> GetByAccountAsync(
        Guid userId,
        string cloudAccountUid,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cloudAccountUid);

        return await db.Queryable<CookieSession>()
            .Where(session => session.UserId == userId && session.CloudAccountUid == cloudAccountUid)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 查询指定用户的全部 Cookie 会话。
    /// </summary>
    public async Task<IReadOnlyList<CookieSession>> GetByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await db.Queryable<CookieSession>()
            .Where(session => session.UserId == userId)
            .OrderBy(session => session.CreationTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 查询全部 Active 或冷却中的 Cookie 会话。
    /// </summary>
    public async Task<IReadOnlyList<CookieSession>> GetAvailableSessionsAsync(
        CancellationToken cancellationToken = default)
    {
        return await db.Queryable<CookieSession>()
            .Where(session => session.Status == Domain.Enums.CookieSessionStatus.Active
                || session.Status == Domain.Enums.CookieSessionStatus.Invalidated)
            .OrderBy(session => session.CreationTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }
}
