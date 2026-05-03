using Domain.Results;

using MediatR;

namespace Application.Queries;

/// <summary>
/// Query 标记接口。表示读操作请求，禁止产生副作用（写库、发布领域事件等）。
/// </summary>
/// <typeparam name="TResponse">查询返回的数据类型。</typeparam>
/// <remarks>
/// Query 请求不经过 TransactionBehavior 与 DomainEventDispatchBehavior 的事务/事件分发逻辑。
/// </remarks>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}
