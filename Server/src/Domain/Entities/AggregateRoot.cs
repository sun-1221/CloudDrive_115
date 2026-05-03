namespace Domain.Entities;

/// <summary>
/// 聚合根标记基类。继承 <see cref="Entity{TId}"/> 的领域事件能力，
/// 语义上表明该实体为聚合的入口点与事务边界。
/// </summary>
/// <typeparam name="TId">聚合根标识类型。</typeparam>
/// <remarks>
/// 外部只能持有聚合根的引用，不能直接持有聚合内部实体的引用。
/// 一个事务只修改一个聚合。
/// </remarks>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
}
