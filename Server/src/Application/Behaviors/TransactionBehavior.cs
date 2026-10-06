using Application.Abstractions;
using Application.Commands;

using MediatR;

namespace Application.Behaviors;

/// <summary>
/// 事务管道行为。仅对 Command 请求开启事务，Query 请求直接放行。
/// </summary>
/// <typeparam name="TRequest">请求类型。</typeparam>
/// <typeparam name="TResponse">响应类型。</typeparam>
/// <remarks>
/// <para>
/// Pipeline 顺序：LoggingBehavior → CloudDriveAccountContextBehavior → ValidationBehavior → <b>TransactionBehavior</b> →
/// DomainEventDispatchBehavior。
/// </para>
/// <para>
/// Command 判定通过检查 <typeparamref name="TRequest"/> 是否实现 <see cref="ICommand"/>
/// 或 <see cref="ICommand{TResponse}"/> 实现。
/// </para>
/// </remarks>
public sealed class TransactionBehavior<TRequest, TResponse>(
    ITransactionManager transactionManager) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly bool IsCommandRequest = DetermineIsCommand();

    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        // Query 跳过事务（R3.5）
        if (!IsCommandRequest)
        {
            return await next(cancellationToken);
        }

        await transactionManager.BeginAsync(cancellationToken);

        try
        {
            TResponse response = await next(cancellationToken);
            await transactionManager.CommitAsync(cancellationToken);
            return response;
        }
        catch
        {
            // Command Handler 异常 → 回滚并重新抛出（R3.7）
            await transactionManager.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static bool DetermineIsCommand()
    {
        Type requestType = typeof(TRequest);

        if (typeof(ICommand).IsAssignableFrom(requestType))
        {
            return true;
        }

        foreach (Type interfaceType in requestType.GetInterfaces())
        {
            if (interfaceType.IsGenericType
                && interfaceType.GetGenericTypeDefinition() == typeof(ICommand<>))
            {
                return true;
            }
        }

        return false;
    }
}
