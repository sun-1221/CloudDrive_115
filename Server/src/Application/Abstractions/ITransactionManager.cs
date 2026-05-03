namespace Application.Abstractions;

/// <summary>
/// 事务管理抽象。定义在 Application 层，由 Infrastructure 层基于 SqlSugar 实现。
/// </summary>
/// <remarks>
/// <para>
/// 由 <c>TransactionBehavior</c> 在 Command 请求管道中统一调用：
/// Handler 执行前调用 <see cref="BeginAsync"/>，成功时调用 <see cref="CommitAsync"/>，
/// 异常时调用 <see cref="RollbackAsync"/>。
/// </para>
/// <para>
/// Query 请求不经过事务管理，本接口不为 Query Handler 所用。
/// </para>
/// </remarks>
public interface ITransactionManager
{
    /// <summary>
    /// 异步开启事务。
    /// </summary>
    Task BeginAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步提交当前事务。
    /// </summary>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步回滚当前事务。
    /// </summary>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
