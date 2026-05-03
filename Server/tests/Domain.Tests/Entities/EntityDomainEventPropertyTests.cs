using Domain.Entities;
using Domain.Events;

using FsCheck.Xunit;

namespace Domain.Tests.Entities;

/// <summary>
/// 设计文档 Property 6：实体领域事件收集。
/// <para>
/// *For any* 实体实例和任意领域事件，调用 <see cref="Entity{TId}.AddDomainEvent"/> 后，
/// 该事件应出现在实体的领域事件集合中，且事件集合的长度应增加 1。
/// </para>
/// </summary>
public sealed class EntityDomainEventPropertyTests
{
    /// <summary>
    /// Property 6：对任意实体与任意领域事件，AddDomainEvent 后事件被追加且计数加 1。
    /// </summary>
    [Property]
    public bool AddDomainEvent_ForAnyEntityAndEvent_AppendsEventAndIncrementsCount(
        Guid entityId,
        int eventPayload)
    {
        // Arrange
        var entity = new TestEntity(entityId);
        int originalCount = entity.DomainEvents.Count;
        var domainEvent = new TestDomainEvent(eventPayload);

        // Act
        entity.AddDomainEvent(domainEvent);

        // Assert (两条性质同时成立)
        return entity.DomainEvents.Contains(domainEvent)
            && entity.DomainEvents.Count == originalCount + 1;
    }

    [Fact]
    public void AddDomainEvent_WithNullEvent_ThrowsArgumentNullException()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => entity.AddDomainEvent(null!));
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllCollectedEvents()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid());
        entity.AddDomainEvent(new TestDomainEvent(1));
        entity.AddDomainEvent(new TestDomainEvent(2));

        // Act
        entity.ClearDomainEvents();

        // Assert
        Assert.Empty(entity.DomainEvents);
    }

    /// <summary>
    /// 测试用实体，使用 <see cref="Guid"/> 作为标识。
    /// </summary>
    private sealed class TestEntity : Entity<Guid>
    {
        public TestEntity(Guid id)
        {
            Id = id;
        }
    }

    /// <summary>
    /// 测试用领域事件。继承 <see cref="DomainEvent"/> 以获得 OccurredOnUtc。
    /// </summary>
    private sealed record TestDomainEvent(int Payload) : DomainEvent;
}
