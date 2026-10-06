using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using WebApi.Middleware;
using WebApi.Tests.Support;

namespace WebApi.Tests.Middleware;

/// <summary>
/// Property 11：异常中间件日志记录。
/// </summary>
/// <remarks>
/// 对应 Property 11（design.md §Property 11）与需求 R10.5：
/// 异常处理后应产生包含请求路径、异常类型和异常消息的日志。
/// </remarks>
public sealed class ExceptionHandlingMiddlewareLoggingPropertyTests
{
    /// <summary>
    /// 任意抛出的异常经过中间件处理后，日志中必定包含请求路径、异常类型全名与异常消息。
    /// </summary>
    [Property(Arbitrary = [typeof(LogPropertyArbs)])]
    public async Task Exception_is_logged_with_path_type_and_message(
        NonEmptyString path,
        NonEmptyString message)
    {
        // Arrange（准备）
        var logger = new FakeLogger<ExceptionHandlingMiddleware>();
        var env = new FakeHostEnvironment();
        string requestPath = "/" + path.Get;
        var exception = new InvalidOperationException(message.Get);
        RequestDelegate next = _ => throw exception;

        var middleware = new ExceptionHandlingMiddleware(next, logger, env);
        var context = new DefaultHttpContext
        {
            Response = { Body = new MemoryStream() },
        };
        context.Request.Path = requestPath;
        string actualPath = context.Request.Path.Value!;

        // Act（执行）
        await middleware.InvokeAsync(context);

        // Assert（断言）
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error);
        LogEntry errorEntry = logger.Entries.First(e => e.Level == LogLevel.Error);

        Assert.Contains(actualPath, errorEntry.Message, StringComparison.Ordinal);
        Assert.Contains(typeof(InvalidOperationException).FullName!, errorEntry.Message, StringComparison.Ordinal);
        Assert.Contains(exception.Message, errorEntry.Message, StringComparison.Ordinal);
        Assert.Same(exception, errorEntry.Exception);
    }
}

/// <summary>FsCheck 仲裁器容器：本测试模块使用。</summary>
public static class LogPropertyArbs
{
    /// <summary>非空字符串。</summary>
    public static Arbitrary<NonEmptyString> NonEmpty()
    {
        Gen<NonEmptyString> gen = ArbMap.Default.ArbFor<NonEmptyString>().Generator;
        return Arb.From(gen);
    }
}
