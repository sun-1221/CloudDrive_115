using Application.Abstractions;

using SqlSugar;

namespace Infrastructure.Persistence;

/// <summary>
/// 基于 SqlSugar 的 <see cref="ITransactionManager"/> 实现。
/// </summary>
/// <remarks>
/// 按设计 §1. SqlSugar + §MediatR Pipeline Behaviors 要求：<br/>
/// - 仅对 Command 请求开启事务（由 <c>TransactionBehavior</c> 保证）；<br/>
/// - 使用同一个 Scoped <see cref="ISqlSugarClient"/> 实例，借助
///   <see cref="IAdo.BeginTranAsync"/> / <see cref="IAdo.CommitTranAsync"/> /
///   <see cref="IAdo.RollbackTranAsync"/> 保证事务语义（R3.4–R3.7）。
/// </remarks>
public sealed class SqlSugarTransactionManager(ISqlSugarClient db) : ITransactionManager
{
    private readonly ISqlSugarClient _db = db ?? throw new ArgumentNullException(nameof(db));

    /// <inheritdoc />
    public Task BeginAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _db.Ado.BeginTranAsync();
    }

    /// <inheritdoc />
    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _db.Ado.CommitTranAsync();
    }

    /// <inheritdoc />
    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _db.Ado.RollbackTranAsync();
    }
}
