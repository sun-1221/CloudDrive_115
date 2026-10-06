namespace WebApi.Middleware;

/// <summary>
/// <see cref="ExceptionHandlingMiddleware"/> 的管道注册扩展。
/// </summary>
public static class ExceptionHandlingMiddlewareExtensions
{
    /// <summary>
    /// 在请求管道最前部启用全局异常处理中间件（R12.2）。
    /// </summary>
    /// <param name="app">应用构建器。</param>
    /// <returns>原始应用构建器。</returns>
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
