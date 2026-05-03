using Microsoft.Extensions.Logging;

namespace WebApi.Tests.Support;

/// <summary>
/// 测试用 <see cref="ILogger{TCategoryName}"/> 实现，收集全部日志条目供断言使用。
/// </summary>
/// <typeparam name="T">日志分类类型。</typeparam>
public sealed class FakeLogger<T> : ILogger<T>
{
    private readonly List<LogEntry> _entries = [];

    /// <summary>
    /// 已写入的日志条目快照。
    /// </summary>
    public IReadOnlyList<LogEntry> Entries => _entries;

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc />
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        string message = formatter(state, exception);
        _entries.Add(new LogEntry(logLevel, message, exception));
    }
}

/// <summary>
/// 日志条目记录。
/// </summary>
/// <param name="Level">日志级别。</param>
/// <param name="Message">格式化后的日志消息。</param>
/// <param name="Exception">关联异常（如有）。</param>
public sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);
