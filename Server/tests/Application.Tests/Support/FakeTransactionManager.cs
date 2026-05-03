using Application.Abstractions;

namespace Application.Tests.Support;

/// <summary>
/// 测试用的 <see cref="ITransactionManager"/> 实现，记录 Begin/Commit/Rollback 调用次数与顺序。
/// </summary>
internal sealed class FakeTransactionManager : ITransactionManager
{
    private readonly List<string> _callOrder = [];

    public int BeginCount { get; private set; }

    public int CommitCount { get; private set; }

    public int RollbackCount { get; private set; }

    public IReadOnlyList<string> CallOrder => _callOrder;

    public Task BeginAsync(CancellationToken cancellationToken = default)
    {
        BeginCount++;
        _callOrder.Add(nameof(BeginAsync));
        return Task.CompletedTask;
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        CommitCount++;
        _callOrder.Add(nameof(CommitAsync));
        return Task.CompletedTask;
    }

    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        RollbackCount++;
        _callOrder.Add(nameof(RollbackAsync));
        return Task.CompletedTask;
    }
}
