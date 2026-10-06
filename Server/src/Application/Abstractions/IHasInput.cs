namespace Application.Abstractions;

/// <summary>
/// 标记接口，表示请求携带可独立验证的输入 DTO。
/// 在 DI 注册时由基础设施层自动扫描并注册
/// <see cref="FluentValidation.IValidator{T}"/> 桥接适配器，
/// 将 <c>IValidator&lt;TInput&gt;</c> 适配为 <c>IValidator&lt;TRequest&gt;</c>。
/// </summary>
/// <typeparam name="TInput">输入 DTO 类型。</typeparam>
public interface IHasInput<TInput>
{
    /// <summary>
    /// 请求携带的输入 DTO。
    /// </summary>
    TInput Input { get; }
}
