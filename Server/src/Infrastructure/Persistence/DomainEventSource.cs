using Application.Abstractions;

using Domain.Entities;

namespace Infrastructure.Persistence;

/// <summary>
/// <see cref="IDomainEventSource"/> 的内存实现。每个请求作用域内独立收集被仓储持久化的聚合，
/// 供 <c>DomainEventDispatchBehavior</c> 统一分发并清空。
/// </summary>
/// <remarks>
/// 按设计 §领域事件 + §MediatR Pipeline Behaviors 要求：<br/>
/// - 追踪调用幂等：同一实体多次 <see cref="Track"/> 仅保留一份引用（R5.7）<br/>
/// - 生命周期应注册为 <see cref="Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped"/>，
///   确保与 Repository、事务边界对齐。<br/>
/// 本实现非线程安全，假设在单个请求的同步串行管道中使用。
/// </remarks>
public sealed class DomainEventSource : IDomainEventSource
{
    // 使用 List + 引用相等去重，避免实体定义自定义 Equals 时产生误合并
    private readonly List<IEntity> _trackedEntities = [];

    /// <inheritdoc />
    public void Track(IEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        foreach (IEntity tracked in _trackedEntities)
        {
            if (ReferenceEquals(tracked, entity))
            {
                return;
            }
        }

        _trackedEntities.Add(entity);
    }

    /// <inheritdoc />
    public IReadOnlyCollection<IEntity> GetTrackedEntities() => _trackedEntities.AsReadOnly();

    /// <inheritdoc />
    public void Clear() => _trackedEntities.Clear();
}
