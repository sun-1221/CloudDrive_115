using Application.Abstractions;
using Application.Commands;
using Application.Events;

using Domain.Entities;
using Domain.Events;

using MediatR;

namespace Application.Behaviors;

/// <summary>
/// 领域事件分发管道行为。仅对 Command 请求生效，在 Handler 执行后、事务提交前
/// 遍历被追踪实体收集的领域事件，包装为 <see cref="DomainEventNotification{TDomainEvent}"/>
/// 后统一发布，发布完成后清空事件集合。
/// </summary>
/// <typeparam name="TRequest">请求类型。</typeparam>
/// <typeparam name="TResponse">响应类型。</typeparam>
/// <remarks>
/// <para>
/// Pipeline 顺序：LoggingBehavior → CloudDriveAccountContextBehavior → ValidationBehavior → TransactionBehavior →
/// <b>DomainEventDispatchBehavior</b>。
/// </para>
/// <para>
/// Query 请求直接放行，不会触发事件分发。
/// 普通 Command Handler 不应再手动调用 <c>IMediator.Publish</c>；领域事件由本行为统一处理。
/// </para>
/// </remarks>
public sealed class DomainEventDispatchBehavior<TRequest, TResponse>(
    IPublisher publisher,
    IDomainEventSource domainEventSource) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly bool IsCommandRequest = DetermineIsCommand();

    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        if (!IsCommandRequest)
        {
            return await next(cancellationToken);
        }

        TResponse response = await next(cancellationToken);

        IReadOnlyCollection<IEntity> entities = domainEventSource.GetTrackedEntities();

        foreach (IEntity entity in entities)
        {
            // 快照副本，避免发布过程中集合被修改导致的迭代异常
            DomainEvent[] events = [.. entity.DomainEvents];

            foreach (DomainEvent domainEvent in events)
            {
                await publisher.Publish((object)DomainEventNotificationFactory.Create(domainEvent), cancellationToken);
            }

            entity.ClearDomainEvents();
        }

        domainEventSource.Clear();

        return response;
    }

    private static bool DetermineIsCommand()
    {
        Type requestType = typeof(TRequest);

        if (typeof(ICommand).IsAssignableFrom(requestType))
        {
            return true;
        }

        foreach (Type interfaceType in requestType.GetInterfaces())
        {
            if (interfaceType.IsGenericType
                && interfaceType.GetGenericTypeDefinition() == typeof(ICommand<>))
            {
                return true;
            }
        }

        return false;
    }
}
