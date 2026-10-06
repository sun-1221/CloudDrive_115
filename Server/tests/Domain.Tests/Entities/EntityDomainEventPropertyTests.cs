using Domain.Entities;
using Domain.Events;

using FsCheck.Xunit;

namespace Domain.Tests.Entities;

/// <summary>
/// 设计文档 Property 6：聚合根领域事件收集。
/// <para>
/// *For any* 聚合根实例和任意领域事件，经聚合根领域方法触发后，
/// 该事件应出现在聚合根的领域事件集合中，且事件集合的长度应增加 1。
/// </para>
/// </summary>
public sealed class EntityDomainEventPropertyTests
{
    /// <summary>
    /// Property 6：对任意聚合根与任意领域事件，RaiseDomainEvent 后事件被追加且计数加 1。
    /// </summary>
    [Property]
    public bool RaiseDomainEvent_ForAnyAggregateAndEvent_AppendsEventAndIncrementsCount(
        Guid entityId,
        int eventPayload)
    {
        // Arrange
        var aggregate = new TestAggregateRoot(entityId);
        int originalCount = aggregate.DomainEvents.Count;
        var domainEvent = new TestDomainEvent(eventPayload);

        // Act
        aggregate.Record(domainEvent);

        // Assert (两条性质同时成立)
        return aggregate.DomainEvents.Contains(domainEvent)
            && aggregate.DomainEvents.Count == originalCount + 1;
    }

    [Fact]
    public void RaiseDomainEvent_WithNullEvent_ThrowsArgumentNullException()
    {
        // Arrange
        var aggregate = new TestAggregateRoot(Guid.NewGuid());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => aggregate.Record(null!));
    }

    [Fact]
    public void ClearDomainEvents_WhenEventsCollected_RemovesAllCollectedEvents()
    {
        // Arrange
        var aggregate = new TestAggregateRoot(Guid.NewGuid());
        aggregate.Record(new TestDomainEvent(1));
        aggregate.Record(new TestDomainEvent(2));

        // Act
        aggregate.ClearDomainEvents();

        // Assert
        Assert.Empty(aggregate.DomainEvents);
    }

    /// <summary>
    /// 测试用聚合根，使用 <see cref="Guid"/> 作为标识。
    /// </summary>
    private sealed class TestAggregateRoot : AggregateRoot<Guid>
    {
        public TestAggregateRoot(Guid id)
        {
            Id = id;
        }

        /// <summary>
        /// 通过聚合根领域方法记录领域事件。
        /// </summary>
        /// <param name="domainEvent">要记录的领域事件。</param>
        public void Record(DomainEvent domainEvent) => RaiseDomainEvent(domainEvent);
    }

    /// <summary>
    /// 测试用领域事件。继承 <see cref="DomainEvent"/> 以获得 OccurredOnUtc。
    /// </summary>
    private sealed record TestDomainEvent(int Payload) : DomainEvent;
}
