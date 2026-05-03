using Domain.Results;

using MediatR;

namespace Application.Queries;

/// <summary>
/// Query Handler 基接口。
/// </summary>
/// <typeparam name="TQuery">被处理的 Query 类型。</typeparam>
/// <typeparam name="TResponse">查询返回的数据类型。</typeparam>
public interface IQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>
{
}
