using Domain.Entities;

namespace Domain.Repositories;

/// <summary>
/// 泛型仓储接口。封装聚合持久化的 CRUD 契约。
/// </summary>
/// <typeparam name="TEntity">聚合根类型，必须继承自 <see cref="Entity{TId}"/>。</typeparam>
/// <typeparam name="TId">聚合根标识类型。</typeparam>
/// <remarks>
/// <para>
/// 本接口定义在 Domain 层，由 Infrastructure 层的 <c>Repository&lt;TEntity, TId&gt;</c>
/// 基于 SqlSugar 实现。仓储只负责聚合的持久化，不承载业务规则。
/// </para>
/// <para>
/// 复杂报表/列表查询不应扩充本接口，应由 Infrastructure 提供专用查询实现
/// 或应用服务依赖的抽象接口。
/// </para>
/// </remarks>
public interface IRepository<TEntity, TId>
    where TEntity : Entity<TId>
    where TId : notnull
{
    /// <summary>
    /// 按标识异步获取聚合。
    /// </summary>
    /// <param name="id">聚合根标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>存在时返回实体；否则返回 <see langword="null"/>。</returns>
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取全部实体的只读列表。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>只读实体列表；无数据时返回空列表。</returns>
    Task<IReadOnlyList<TEntity>> GetListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步插入实体。
    /// </summary>
    /// <param name="entity">要插入的实体，不能为 <see langword="null"/>。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>实际写入的行数（通常为 1）。</returns>
    Task<int> InsertAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步更新实体。
    /// </summary>
    /// <param name="entity">要更新的实体，不能为 <see langword="null"/>。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>实际更新的行数。</returns>
    Task<int> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步删除实体。
    /// </summary>
    /// <param name="entity">要删除的实体，不能为 <see langword="null"/>。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>实际删除的行数。</returns>
    Task<int> DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
}
