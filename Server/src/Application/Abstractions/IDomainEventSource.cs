using Domain.Entities;

namespace Application.Abstractions;

/// <summary>
/// 领域事件源抽象。用于收集在当前请求中被仓储持久化的聚合，供
/// <c>DomainEventDispatchBehavior</c> 统一分发其领域事件。
/// </summary>
/// <remarks>
/// <para>
/// 定义在 Application 层、实现在 Infrastructure 层（基于 SqlSugar）。
/// 仓储在 Insert/Update 等持久化成功后调用 <see cref="Track"/> 追踪聚合。
/// </para>
/// <para>
/// 生命周期建议：Scoped，每个请求一个实例。
/// </para>
/// </remarks>
public interface IDomainEventSource
{
    /// <summary>
    /// 追踪一个包含挂起领域事件的聚合。重复追踪同一实例应幂等。
    /// </summary>
    /// <param name="entity">要追踪的实体聚合。</param>
    void Track(IEntity entity);

    /// <summary>
    /// 获取当前已追踪的全部实体的只读快照。
    /// </summary>
    /// <returns>被追踪实体集合，未追踪任何实体时返回空集合。</returns>
    IReadOnlyCollection<IEntity> GetTrackedEntities();

    /// <summary>
    /// 清空已追踪实体集合，通常由事件分发器在本次请求结束时调用。
    /// </summary>
    void Clear();
}
