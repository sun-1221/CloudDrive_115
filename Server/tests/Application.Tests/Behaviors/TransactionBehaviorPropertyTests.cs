using Application.Behaviors;
using Application.Commands;
using Application.Queries;
using Application.Tests.Support;

using Domain.Results;

using FsCheck.Xunit;

namespace Application.Tests.Behaviors;

/// <summary>
/// 设计文档 Property 3 与 Property 4：TransactionBehavior 行为属性测试。
/// </summary>
public sealed class TransactionBehaviorPropertyTests
{
    /// <summary>
    /// Property 3：对任意 Command 请求 TransactionBehavior 应开启事务；
    /// 对任意 Query 请求应跳过事务直接执行后续管道。
    /// </summary>
    [Property]
    public async Task<bool> Handle_CommandOpensTransaction_QuerySkips(int payload)
    {
        // --- Command 分支 ---
        var commandTxn = new FakeTransactionManager();
        var commandBehavior = new TransactionBehavior<TestCommand, Result>(commandTxn);
        _ = await commandBehavior.Handle(
            new TestCommand(payload),
            _ => Task.FromResult(Result.Success()),
            CancellationToken.None);

        bool commandBegan = commandTxn.BeginCount == 1;
        bool commandCommitted = commandTxn.CommitCount == 1;
        bool commandNotRolledBack = commandTxn.RollbackCount == 0;

        // --- Query 分支 ---
        var queryTxn = new FakeTransactionManager();
        var queryBehavior = new TransactionBehavior<TestQuery, Result<int>>(queryTxn);
        _ = await queryBehavior.Handle(
            new TestQuery(payload),
            _ => Task.FromResult(Result.Success(payload)),
            CancellationToken.None);

        bool querySkippedEverything = queryTxn.BeginCount == 0
            && queryTxn.CommitCount == 0
            && queryTxn.RollbackCount == 0;

        return commandBegan && commandCommitted && commandNotRolledBack && querySkippedEverything;
    }

    /// <summary>
    /// Property 4：Command Handler 成功时事务提交；异常时事务回滚并重新抛出异常。
    /// </summary>
    [Property]
    public async Task<bool> Handle_CommitsOnSuccess_RollsBackOnException(int payload)
    {
        // --- 成功分支 ---
        var successTxn = new FakeTransactionManager();
        var successBehavior = new TransactionBehavior<TestCommand, Result>(successTxn);
        _ = await successBehavior.Handle(
            new TestCommand(payload),
            _ => Task.FromResult(Result.Success()),
            CancellationToken.None);

        bool successCommittedAndNotRolledBack =
            successTxn.BeginCount == 1
            && successTxn.CommitCount == 1
            && successTxn.RollbackCount == 0
            && successTxn.CallOrder is ["BeginAsync", "CommitAsync"];

        // --- 异常分支 ---
        var failureTxn = new FakeTransactionManager();
        var failureBehavior = new TransactionBehavior<TestCommand, Result>(failureTxn);
        var expectedException = new InvalidOperationException($"boom-{payload}");

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            failureBehavior.Handle(
                new TestCommand(payload),
                _ => Task.FromException<Result>(expectedException),
                CancellationToken.None));

        bool failureRolledBackAndRethrew =
            failureTxn.BeginCount == 1
            && failureTxn.CommitCount == 0
            && failureTxn.RollbackCount == 1
            && failureTxn.CallOrder is ["BeginAsync", "RollbackAsync"]
            && ReferenceEquals(actual, expectedException);

        return successCommittedAndNotRolledBack && failureRolledBackAndRethrew;
    }

    public sealed record TestCommand(int Payload) : ICommand;

    public sealed record TestQuery(int Payload) : IQuery<int>;
}
