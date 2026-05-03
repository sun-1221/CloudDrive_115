using Domain.Results;

using MediatR;

namespace Application.Commands;

/// <summary>
/// Command Handler 基接口（无返回值）。
/// </summary>
/// <typeparam name="TCommand">被处理的 Command 类型，必须实现 <see cref="ICommand"/>。</typeparam>
public interface ICommandHandler<TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand
{
}

/// <summary>
/// Command Handler 基接口（带返回值）。
/// </summary>
/// <typeparam name="TCommand">被处理的 Command 类型。</typeparam>
/// <typeparam name="TResponse">Handler 返回的业务数据类型。</typeparam>
public interface ICommandHandler<TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>
{
}
