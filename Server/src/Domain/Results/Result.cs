namespace Domain.Results;

/// <summary>
/// 操作结果模式基类。区分成功/失败两种状态，失败时携带 <see cref="Error"/>。
/// </summary>
/// <remarks>
/// Handler 应返回 <see cref="Result"/> / <see cref="Result{T}"/> 表达业务错误；
/// 基础设施异常（数据库、网络）仍以异常形式抛出。
/// </remarks>
public class Result
{
    /// <summary>
    /// 构造函数。仅允许 (成功,None) 或 (失败,非 None) 组合，防止状态不一致。
    /// </summary>
    /// <param name="isSuccess">是否成功。</param>
    /// <param name="error">关联错误。</param>
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
        {
            throw new InvalidOperationException("Success result cannot carry an error.");
        }

        if (!isSuccess && error == Error.None)
        {
            throw new InvalidOperationException("Failure result must carry a non-empty error.");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>
    /// 当前结果是否为成功状态。
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// 当前结果是否为失败状态。
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// 关联错误；成功时为 <see cref="Error.None"/>。
    /// </summary>
    public Error Error { get; }

    /// <summary>
    /// 创建成功结果。
    /// </summary>
    /// <returns>成功的 <see cref="Result"/>。</returns>
    public static Result Success() => new(true, Error.None);

    /// <summary>
    /// 创建失败结果。
    /// </summary>
    /// <param name="error">失败原因。</param>
    /// <returns>失败的 <see cref="Result"/>。</returns>
    public static Result Failure(Error error) => new(false, error);

    /// <summary>
    /// 创建带返回值的成功结果。
    /// </summary>
    /// <typeparam name="T">返回值类型。</typeparam>
    /// <param name="value">成功值。</param>
    /// <returns>成功的 <see cref="Result{T}"/>。</returns>
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);

    /// <summary>
    /// 创建带返回值类型的失败结果。
    /// </summary>
    /// <typeparam name="T">返回值类型。</typeparam>
    /// <param name="error">失败原因。</param>
    /// <returns>失败的 <see cref="Result{T}"/>。</returns>
    public static Result<T> Failure<T>(Error error) => Result<T>.Failure(error);
}

/// <summary>
/// 携带返回值的操作结果。
/// </summary>
/// <typeparam name="T">成功时的返回值类型。</typeparam>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    /// <summary>
    /// 以返回值、成功标志和错误构造结果。
    /// </summary>
    /// <param name="value">成功值。</param>
    /// <param name="isSuccess">是否成功。</param>
    /// <param name="error">关联错误。</param>
    private Result(T? value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>
    /// 成功时的返回值；失败时访问该属性将抛出异常。
    /// </summary>
    /// <exception cref="InvalidOperationException">当结果为失败状态时抛出。</exception>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value on a failure result.");

    /// <summary>
    /// 创建成功结果并包装返回值。
    /// </summary>
    /// <param name="value">成功值。</param>
    /// <returns>成功的 <see cref="Result{T}"/>。</returns>
    public static Result<T> Success(T value) => new(value, true, Error.None);

    /// <summary>
    /// 创建失败结果。
    /// </summary>
    /// <param name="error">失败原因。</param>
    /// <returns>失败的 <see cref="Result{T}"/>。</returns>
    public static new Result<T> Failure(Error error) => new(default, false, error);
}
