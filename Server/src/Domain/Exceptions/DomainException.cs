namespace Domain.Exceptions;

/// <summary>
/// 领域/业务异常基类。业务模块通过派生本类或约定 <see cref="StatusCode"/>，
/// 由全局异常处理中间件映射为对应 HTTP 状态码（R10.3）。
/// </summary>
/// <remarks>
/// <para>
/// 派生异常应根据语义设置 <see cref="StatusCode"/>，典型值：<br/>
/// - 400 BadRequest<br/>
/// - 401 Unauthorized<br/>
/// - 403 Forbidden<br/>
/// - 404 NotFound<br/>
/// - 409 Conflict<br/>
/// - 429 TooManyRequests<br/>
/// - 502 BadGateway<br/>
/// - 503 ServiceUnavailable
/// </para>
/// <para>
/// 领域层不直接依赖 <see cref="System.Net.HttpStatusCode"/>，而以 int 表达状态码，
/// 由 WebApi 层中间件负责映射。<see cref="ErrorCode"/> 可选，用于业务错误分类。
/// </para>
/// </remarks>
public abstract class DomainException : Exception
{
    /// <summary>
    /// 异常对应的 HTTP 状态码。
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// 业务错误码（可选），供前端或调用方做错误分类。
    /// </summary>
    public string? ErrorCode { get; }

    /// <summary>
    /// 以状态码、错误码与消息构造领域异常。
    /// </summary>
    protected DomainException(int statusCode, string message, string? errorCode = null)
        : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }

    /// <summary>
    /// 以状态码、错误码、消息和内部异常构造领域异常。
    /// </summary>
    protected DomainException(int statusCode, string message, Exception innerException, string? errorCode = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }
}
