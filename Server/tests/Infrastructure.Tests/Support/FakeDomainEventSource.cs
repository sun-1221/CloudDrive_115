using Application.Abstractions;

using Domain.Entities;

namespace Infrastructure.Tests.Support;

/// <summary>
/// 测试用 <see cref="IDomainEventSource"/> 实现，收集 <see cref="Track"/> 调用的实体。
/// </summary>
internal sealed class FakeDomainEventSource : IDomainEventSource
{
    private readonly List<IEntity> _tracked = [];

    public IReadOnlyList<IEntity> Tracked => _tracked;

    public void Track(IEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _tracked.Add(entity);
    }

    public IReadOnlyCollection<IEntity> GetTrackedEntities() => _tracked;

    public void Clear() => _tracked.Clear();
}
