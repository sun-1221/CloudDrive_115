using System.Reflection;

using FluentValidation;
using FluentValidation.Results;

using MediatR;

namespace Application.Behaviors;

/// <summary>
/// 验证管道行为。自动执行已注册的 FluentValidation 验证器，校验失败时抛出
/// </summary>
/// <typeparam name="TRequest">请求类型。</typeparam>
/// <typeparam name="TResponse">响应类型。</typeparam>
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators,
    IServiceProvider serviceProvider) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly PropertyInfo? InputProperty = typeof(TRequest).GetProperty("Input");

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        List<ValidationFailure> allFailures = [];

        // 运行直接针对 TRequest 的验证器
        await RunDirectValidatorsAsync(request, allFailures, cancellationToken);

        // 运行 Input 属性对应类型的验证器（自动桥接）
        await RunInputPropertyValidatorsAsync(request, allFailures, cancellationToken);

        if (allFailures.Count > 0)
        {
            throw new ValidationException(allFailures);
        }

        return await next(cancellationToken);
    }

    private async Task RunDirectValidatorsAsync(
        TRequest request,
        List<ValidationFailure> failures,
        CancellationToken cancellationToken)
    {
        IValidator<TRequest>[] validatorArray = [.. validators];
        if (validatorArray.Length == 0)
        {
            return;
        }

        var context = new ValidationContext<TRequest>(request);
        ValidationResult[] results = await Task.WhenAll(
            validatorArray.Select(v => v.ValidateAsync(context, cancellationToken)));

        failures.AddRange(results.SelectMany(r => r.Errors).Where(f => f is not null));
    }

    private async Task RunInputPropertyValidatorsAsync(
        TRequest request,
        List<ValidationFailure> failures,
        CancellationToken cancellationToken)
    {
        if (InputProperty is null)
        {
            return;
        }

        object? inputValue = InputProperty.GetValue(request);
        if (inputValue is null)
        {
            return;
        }

        Type inputType = InputProperty.PropertyType;
        Type enumerableType = typeof(IEnumerable<>).MakeGenericType(
            typeof(IValidator<>).MakeGenericType(inputType));

        if (serviceProvider.GetService(enumerableType) is not System.Collections.IEnumerable resolvedValidators)
        {
            return;
        }

        Type contextType = typeof(ValidationContext<>).MakeGenericType(inputType);
        IValidationContext validationContext = (IValidationContext)Activator.CreateInstance(contextType, inputValue)!;

        foreach (object? validator in resolvedValidators)
        {
            if (validator is IValidator typedValidator)
            {
                ValidationResult result = await typedValidator.ValidateAsync(validationContext, cancellationToken);
                failures.AddRange(result.Errors.Where(f => f is not null));
            }
        }
    }
}
