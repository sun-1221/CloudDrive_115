using MediatR;

using Domain.Events;

namespace Application.Events;

/// <summary>
/// 将纯领域事件包装为 MediatR 通知，使领域层无需依赖 MediatR。
/// </summary>
/// <typeparam name="TDomainEvent">被包装的领域事件类型。</typeparam>
/// <param name="DomainEvent">被包装的领域事件实例。</param>
public sealed record DomainEventNotification<TDomainEvent>(TDomainEvent DomainEvent) : INotification
    where TDomainEvent : DomainEvent;
