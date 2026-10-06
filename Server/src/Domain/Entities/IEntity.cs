using Domain.Events;

namespace Domain.Entities;

/// <summary>
/// 实体通用契约。暴露领域事件访问与清空能力，供 Application 层在不依赖具体标识类型的情况下统一处理。
/// </summary>
/// <remarks>
/// <see cref="Entity{TId}"/> 实现本接口。
/// 本接口由 <c>IDomainEventSource</c> 追踪聚合并由 <c>DomainEventDispatchBehavior</c> 发布事件时使用。
/// </remarks>
public interface IEntity
{
    /// <summary>
    /// 当前实体挂起的领域事件集合（只读视图）。
    /// </summary>
    IReadOnlyList<DomainEvent> DomainEvents { get; }

    /// <summary>
    /// 清空实体的领域事件集合，通常由事件分发器在发布完成后调用。
    /// </summary>
    void ClearDomainEvents();
}
