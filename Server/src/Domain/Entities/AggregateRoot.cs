using Domain.Events;

namespace Domain.Entities;

/// <summary>
/// 聚合根基类。作为聚合入口点与事务边界，负责收集领域事件。
/// </summary>
/// <typeparam name="TId">聚合根标识类型。</typeparam>
/// <remarks>
/// 外部只能持有聚合根的引用，不能直接持有聚合内部实体的引用。
/// 一个事务只修改一个聚合；领域事件只能由聚合根领域方法通过 <see cref="RaiseDomainEvent"/> 收集。
/// </remarks>
public abstract class AggregateRoot<TId> : Entity<TId>, IEntity
    where TId : notnull
{
    private readonly List<DomainEvent> _domainEvents = [];

    /// <summary>
    /// 当前聚合根挂起的领域事件集合（只读视图）。
    /// </summary>
    public IReadOnlyList<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// 由聚合根领域方法收集一个待发布的领域事件。
    /// </summary>
    /// <param name="domainEvent">要收集的领域事件，不能为 <see langword="null"/>。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="domainEvent"/> 为 <see langword="null"/> 时抛出。</exception>
    protected void RaiseDomainEvent(DomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// 清空聚合根的领域事件集合，通常由事件分发器在发布完成后调用。
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}
