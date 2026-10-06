namespace Domain.Entities;

/// <summary>
/// 实体基类。提供领域对象的唯一标识。
/// </summary>
/// <typeparam name="TId">实体标识类型，要求非空。</typeparam>
/// <remarks>
/// 领域事件能力只暴露在 <see cref="AggregateRoot{TId}"/> 上，避免普通实体或外部调用方绕过聚合根边界。
/// </remarks>
public abstract class Entity<TId>
    where TId : notnull
{
    /// <summary>
    /// 实体唯一标识。
    /// </summary>
    public TId Id { get; protected set; } = default!;
}
