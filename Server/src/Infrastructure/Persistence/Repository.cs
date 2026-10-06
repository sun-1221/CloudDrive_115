using Application.Abstractions;

using Domain.Entities;
using Domain.Repositories;

using SqlSugar;

namespace Infrastructure.Persistence;

/// <summary>
/// 泛型仓储基类。基于 SqlSugar 实现 <see cref="IRepository{TEntity, TId}"/>，
/// 并在持久化成功后通过 <see cref="IDomainEventSource"/> 追踪聚合，供
/// <c>DomainEventDispatchBehavior</c> 统一分发领域事件（R5.7）。
/// </summary>
/// <typeparam name="TEntity">聚合根类型。</typeparam>
/// <typeparam name="TId">聚合根标识类型。</typeparam>
internal sealed class Repository<TEntity, TId>(
    ISqlSugarClient db,
    IDomainEventSource domainEventSource) : IRepository<TEntity, TId>
    where TEntity : AggregateRoot<TId>, new()
    where TId : notnull
{
    /// <summary>
    /// 底层 SqlSugar 客户端。
    /// </summary>
    private ISqlSugarClient Db { get; } = db;

    /// <summary>
    /// 领域事件源，供持久化后追踪聚合使用。
    /// </summary>
    private IDomainEventSource DomainEventSource { get; } = domainEventSource;

    /// <inheritdoc />
    public async Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await Db.Queryable<TEntity>().InSingleAsync(id);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TEntity>> GetListAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        List<TEntity> list = await Db.Queryable<TEntity>().ToListAsync(cancellationToken);
        return list;
    }

    /// <inheritdoc />
    public async Task<int> InsertAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        int rows = await Db.Insertable(entity).ExecuteCommandAsync(cancellationToken);
        if (rows > 0)
        {
            DomainEventSource.Track(entity);
        }

        return rows;
    }

    /// <inheritdoc />
    public async Task<int> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        int rows = await Db.Updateable(entity).ExecuteCommandAsync(cancellationToken);
        if (rows > 0)
        {
            DomainEventSource.Track(entity);
        }

        return rows;
    }

    /// <inheritdoc />
    public async Task<int> DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        int rows = await Db.Deleteable(entity).ExecuteCommandAsync(cancellationToken);
        if (rows > 0)
        {
            DomainEventSource.Track(entity);
        }

        return rows;
    }
}
