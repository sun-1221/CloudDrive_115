using FluentValidation;
using FluentValidation.Results;

using MediatR;

namespace Application.Behaviors;

/// <summary>
/// 验证管道行为。自动执行已注册的 FluentValidation 验证器，校验失败时抛出
/// <see cref="ValidationException"/>。
/// </summary>
/// <remarks>
/// Input DTO 验证由 <see cref="InputPropertyValidator{TRequest, TInput}"/> 桥接适配器自动完成，
/// 在 DI 注册阶段已将 <c>IValidator&lt;TInput&gt;</c> 适配为 <c>IValidator&lt;TRequest&gt;</c>，
/// 因此本 Behavior 无需依赖 <see cref="IServiceProvider"/> 或运行时反射。
/// </remarks>
/// <typeparam name="TRequest">请求类型。</typeparam>
/// <typeparam name="TResponse">响应类型。</typeparam>
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        IValidator<TRequest>[] validatorArray = [.. validators];
        if (validatorArray.Length == 0)
        {
            return await next(cancellationToken);
        }

        var context = new ValidationContext<TRequest>(request);
        ValidationResult[] results = await Task.WhenAll(
            validatorArray.Select(v => v.ValidateAsync(context, cancellationToken)));

        List<ValidationFailure> failures = results
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }

        return await next(cancellationToken);
    }
}
