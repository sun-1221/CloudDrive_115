using System.Diagnostics;

using MediatR;

using Microsoft.Extensions.Logging;

namespace Application.Behaviors;

/// <summary>
/// 日志管道行为。为每个 MediatR 请求记录请求类型、响应耗时与异常信息。
/// </summary>
/// <typeparam name="TRequest">请求类型。</typeparam>
/// <typeparam name="TResponse">响应类型。</typeparam>
/// <remarks>
/// 作用于所有 Command 与 Query，是 Pipeline 的第一层（LoggingBehavior → ValidationBehavior →
/// TransactionBehavior → DomainEventDispatchBehavior）。
/// 使用静态日志模板（CA2254），不序列化完整请求以避免敏感字段泄露。
/// </remarks>
public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        string requestName = typeof(TRequest).Name;
        logger.LogInformation("开始处理请求 {RequestName}", requestName);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            TResponse response = await next(cancellationToken);
            stopwatch.Stop();
            logger.LogInformation(
                "完成处理请求 {RequestName}，耗时 {ElapsedMs} ms",
                requestName,
                stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(
                ex,
                "处理请求 {RequestName} 失败，耗时 {ElapsedMs} ms",
                requestName,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
