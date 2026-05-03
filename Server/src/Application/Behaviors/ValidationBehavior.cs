using FluentValidation;
using FluentValidation.Results;

using MediatR;

namespace Application.Behaviors;

/// <summary>
/// 验证管道行为。自动执行已注册的 FluentValidation 验证器，校验失败时抛出
/// <see cref="ValidationException"/>，阻止 Handler 继续执行。
/// </summary>
/// <typeparam name="TRequest">请求类型。</typeparam>
/// <typeparam name="TResponse">响应类型。</typeparam>
/// <remarks>
/// <para>
/// 作用于所有 Command 与 Query。Pipeline 顺序：LoggingBehavior → <b>ValidationBehavior</b> →
/// TransactionBehavior → DomainEventDispatchBehavior。
/// </para>
/// <para>
/// 当请求没有对应的验证器时直接放行，符合需求 R8.4。
/// </para>
/// </remarks>
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <inheritdoc />
    /// <exception cref="ValidationException">当任一验证器校验失败时，携带所有字段级错误抛出。</exception>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        // 无验证器 → 直接放行（R8.4）
        IValidator<TRequest>[] validatorArray = [.. validators];
        if (validatorArray.Length == 0)
        {
            return await next(cancellationToken);
        }

        var context = new ValidationContext<TRequest>(request);
        ValidationResult[] results = await Task.WhenAll(
            validatorArray.Select(v => v.ValidateAsync(context, cancellationToken)));

        ValidationFailure[] failures = [.. results
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)];

        if (failures.Length > 0)
        {
            throw new ValidationException(failures);
        }

        return await next(cancellationToken);
    }
}
