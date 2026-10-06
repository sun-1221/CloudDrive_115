using Domain.Events;

using MediatR;

namespace Application.Events;

/// <summary>
/// 将纯领域事件包装为 MediatR 通知，使领域层无需依赖 MediatR。
/// </summary>
/// <typeparam name="TDomainEvent">被包装的领域事件类型。</typeparam>
/// <param name="DomainEvent">被包装的领域事件实例。</param>
public sealed record DomainEventNotification<TDomainEvent>(TDomainEvent DomainEvent) : INotification
    where TDomainEvent : DomainEvent;

/// <summary>
/// 将纯领域事件包装为 MediatR 通知的工厂方法。
/// </summary>
public static class DomainEventNotificationFactory
{
    /// <summary>
    /// 将运行时领域事件实例包装为对应的 <see cref="DomainEventNotification{TDomainEvent}"/>。
    /// </summary>
    /// <param name="domainEvent">领域事件实例。</param>
    /// <returns>MediatR 通知实例。</returns>
    public static INotification Create(DomainEvent domainEvent)
    {
        Type notificationType = typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
        return (INotification)Activator.CreateInstance(notificationType, domainEvent)!;
    }
}
