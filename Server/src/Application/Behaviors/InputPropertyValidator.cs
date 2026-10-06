using Application.Abstractions;

using FluentValidation;

namespace Application.Behaviors;

/// <summary>
/// 桥接适配器验证器。将已注册的 <see cref="IValidator{TInput}"/> 自动应用于
/// 实现 <see cref="IHasInput{TInput}"/> 的请求的 <c>Input</c> 属性，
/// 从而消除运行时反射和 <see cref="IServiceProvider"/> 服务定位器。
/// </summary>
/// <typeparam name="TRequest">实现 <see cref="IHasInput{TInput}"/> 的请求类型。</typeparam>
/// <typeparam name="TInput">输入 DTO 类型。</typeparam>
internal sealed class InputPropertyValidator<TRequest, TInput> : AbstractValidator<TRequest>
    where TRequest : IHasInput<TInput>
{
    /// <summary>
    /// 初始化 <see cref="InputPropertyValidator{TRequest, TInput}"/> 类的新实例。
    /// </summary>
    /// <param name="inputValidators">已注册的输入 DTO 验证器集合。</param>
    public InputPropertyValidator(IEnumerable<IValidator<TInput>> inputValidators)
    {
        foreach (IValidator<TInput> validator in inputValidators)
        {
            RuleFor(x => x.Input).SetValidator(validator);
        }
    }
}
