using Application.Abstractions;
using Application.Behaviors;
using Application.Commands;
using Application.Events;
using Application.Queries;

using Domain.Entities;
using Domain.Events;
using Domain.Results;

using MediatR;

namespace Application.Tests.Behaviors;

public sealed class DomainEventDispatchBehaviorTests
{
    [Fact]
    public async Task Handle_WhenCommandHasTrackedDomainEvents_PublishesTypedNotificationsAndClearsTrackedEvents()
    {
        // Arrange
        var publisher = new FakePublisher();
        var eventSource = new FakeDomainEventSource();
        var entity = new TestAggregateRoot(Guid.NewGuid());
        var domainEvent = new TestDomainEvent("created");

        entity.Record(domainEvent);
        eventSource.Track(entity);

        var behavior = new DomainEventDispatchBehavior<TestCommand, Result>(publisher, eventSource);

        // Act
        Result result = await behavior.Handle(
            new TestCommand(),
            _ => Task.FromResult(Result.Success()),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, publisher.ObjectPublishCount);
        Assert.Equal(0, publisher.GenericPublishCount);

        var notification = Assert.IsType<DomainEventNotification<TestDomainEvent>>(
            Assert.Single(publisher.PublishedNotifications));
        Assert.Same(domainEvent, notification.DomainEvent);

        Assert.Empty(entity.DomainEvents);
        Assert.Empty(eventSource.GetTrackedEntities());
    }

    [Fact]
    public async Task Handle_WhenRequestIsQuery_SkipsDomainEventDispatch()
    {
        // Arrange
        var publisher = new FakePublisher();
        var eventSource = new FakeDomainEventSource();
        var entity = new TestAggregateRoot(Guid.NewGuid());

        entity.Record(new TestDomainEvent("created"));
        eventSource.Track(entity);

        var behavior = new DomainEventDispatchBehavior<TestQuery, Result<int>>(publisher, eventSource);

        // Act
        Result<int> result = await behavior.Handle(
            new TestQuery(),
            _ => Task.FromResult(Result.Success(1)),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(publisher.PublishedNotifications);
        Assert.Single(entity.DomainEvents);
        Assert.Single(eventSource.GetTrackedEntities());
    }

    private sealed record TestCommand : ICommand;

    private sealed record TestQuery : IQuery<int>;

    private sealed record TestDomainEvent(string Name) : DomainEvent;

    private sealed class TestAggregateRoot : AggregateRoot<Guid>
    {
        public TestAggregateRoot(Guid id)
        {
            Id = id;
        }

        public void Record(DomainEvent domainEvent) => RaiseDomainEvent(domainEvent);
    }

    private sealed class FakeDomainEventSource : IDomainEventSource
    {
        private readonly List<IEntity> _trackedEntities = [];

        public void Track(IEntity entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            _trackedEntities.Add(entity);
        }

        public IReadOnlyCollection<IEntity> GetTrackedEntities() => _trackedEntities;

        public void Clear() => _trackedEntities.Clear();
    }

    private sealed class FakePublisher : IPublisher
    {
        public int ObjectPublishCount { get; private set; }

        public int GenericPublishCount { get; private set; }

        public List<object> PublishedNotifications { get; } = [];

        public Task Publish(object notification, CancellationToken cancellationToken = default)
        {
            ObjectPublishCount++;
            PublishedNotifications.Add(notification);
            return Task.CompletedTask;
        }

        public Task Publish<TNotification>(
            TNotification notification,
            CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            GenericPublishCount++;
            PublishedNotifications.Add(notification);
            return Task.CompletedTask;
        }
    }
}
