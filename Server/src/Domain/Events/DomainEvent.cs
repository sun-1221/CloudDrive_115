namespace Domain.Events;

/// <summary>
/// 领域事件抽象基类。表达领域内已发生的事实，供应用层统一分发。
/// </summary>
/// <remarks>
/// 领域事件表达领域内已发生的事实，命名为过去式（如 <c>OrderCreatedDomainEvent</c>）。
/// 具体事件应使用 <c>sealed record</c> 继承本类，以获得不可变性与结构相等性。
/// </remarks>
public abstract record DomainEvent
{
    /// <summary>
    /// 事件发生时间（UTC）。
    /// </summary>
    public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
}
