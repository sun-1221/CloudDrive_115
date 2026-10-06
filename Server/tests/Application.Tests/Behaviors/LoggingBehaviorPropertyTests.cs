using Application.Behaviors;
using Application.Tests.Support;

using FsCheck.Xunit;

using MediatR;

using Microsoft.Extensions.Logging;

namespace Application.Tests.Behaviors;

/// <summary>
/// 设计文档 Property 5：LoggingBehavior 记录请求类型与耗时。
/// <para>
/// *For any* MediatR 请求，经过 <see cref="LoggingBehavior{TRequest,TResponse}"/> 处理后应产生日志记录，
/// 包含请求类型名称和响应耗时信息。
/// </para>
/// </summary>
public sealed class LoggingBehaviorPropertyTests
{
    /// <summary>
    /// Property 5：对任意请求，LoggingBehavior 处理后至少产生两条日志（开始 + 完成），
    /// 所有条目的结构化状态包含 <c>RequestName</c>，且至少一条包含 <c>ElapsedMs</c>。
    /// </summary>
    [Property]
    public async Task<bool> Handle_ForAnyRequest_LogsRequestNameAndElapsedMs(int payload)
    {
        // Arrange
        var logger = new FakeLogger<LoggingBehavior<TestRequest, TestResponse>>();
        var behavior = new LoggingBehavior<TestRequest, TestResponse>(logger);
        var request = new TestRequest(payload);

        // Act
        _ = await behavior.Handle(
            request,
            _ => Task.FromResult(new TestResponse(payload)),
            CancellationToken.None);

        // Assert - 两条断言均需成立
        bool atLeastTwoLogs = logger.Entries.Count >= 2;

        bool allHaveRequestName = logger.Entries.All(e =>
            e.State.TryGetValue("RequestName", out object? value)
            && value is string s
            && s == nameof(TestRequest));

        bool someHaveElapsedMs = logger.Entries.Any(e =>
            e.State.TryGetValue("ElapsedMs", out object? value)
            && value is long ms
            && ms >= 0);

        return atLeastTwoLogs && allHaveRequestName && someHaveElapsedMs;
    }

    [Fact]
    public async Task Handle_WhenHandlerThrows_LogsErrorWithRequestNameAndElapsedMs()
    {
        // Arrange（准备）
        var logger = new FakeLogger<LoggingBehavior<TestRequest, TestResponse>>();
        var behavior = new LoggingBehavior<TestRequest, TestResponse>(logger);
        var request = new TestRequest(1);
        var expectedException = new InvalidOperationException("boom");

        // Act（执行）
        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => behavior.Handle(
                request,
                _ => Task.FromException<TestResponse>(expectedException),
                CancellationToken.None));

        // Assert（断言）
        Assert.Same(expectedException, actual);
        FakeLogEntry errorEntry = Assert.Single(logger.Entries, e => e.Level == LogLevel.Error);
        Assert.Equal(nameof(TestRequest), errorEntry.State["RequestName"]);
        Assert.True(errorEntry.State.ContainsKey("ElapsedMs"));
        Assert.Same(expectedException, errorEntry.Exception);
    }

    public sealed record TestRequest(int Payload) : IRequest<TestResponse>;

    public sealed record TestResponse(int Value);
}
