using Domain.Results;

using MediatR;

namespace Application.Commands;

/// <summary>
/// Command 标记接口（无返回值）。表示写操作请求，底层基于 MediatR <see cref="IRequest{TResponse}"/>。
/// </summary>
/// <remarks>
/// <para>
/// 标记作用：<c>TransactionBehavior</c> 与 <c>DomainEventDispatchBehavior</c> 通过检测
/// <see cref="ICommand"/> / <see cref="ICommand{TResponse}"/> 判断请求类型，仅对 Command 开启事务
/// 并在 Handler 执行后分发领域事件，Query 请求跳过。
/// </para>
/// </remarks>
public interface ICommand : IRequest<Result>
{
}

/// <summary>
/// Command 标记接口（带返回值）。
/// </summary>
/// <typeparam name="TResponse">Handler 返回的业务数据类型。</typeparam>
public interface ICommand<TResponse> : IRequest<Result<TResponse>>
{
}
