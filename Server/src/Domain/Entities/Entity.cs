using Domain.Events;

namespace Domain.Entities;

/// <summary>
/// 实体基类。提供唯一标识与领域事件收集、清空能力。
/// </summary>
/// <typeparam name="TId">实体标识类型，要求非空。</typeparam>
/// <remarks>
/// 领域事件由业务方法通过 <see cref="AddDomainEvent"/> 收集，
/// 由 <c>DomainEventDispatchBehavior</c> 在事务边界内统一发布，
/// 发布完成后由上层调用 <see cref="ClearDomainEvents"/> 清空。
/// </remarks>
public abstract class Entity<TId> : IEntity
    where TId : notnull
{
    private readonly List<DomainEvent> _domainEvents = [];

    /// <summary>
    /// 实体唯一标识。
    /// </summary>
    public TId Id { get; protected set; } = default!;

    /// <summary>
    /// 当前实体挂起的领域事件集合（只读视图）。
    /// </summary>
    public IReadOnlyList<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// 向实体追加一个待发布的领域事件。
    /// </summary>
    /// <param name="domainEvent">要追加的领域事件，不能为 <see langword="null"/>。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="domainEvent"/> 为 <see langword="null"/> 时抛出。</exception>
    public void AddDomainEvent(DomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// 清空实体的领域事件集合，通常由事件分发器在发布完成后调用。
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}
