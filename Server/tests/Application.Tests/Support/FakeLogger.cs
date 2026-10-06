using Microsoft.Extensions.Logging;

namespace Application.Tests.Support;

/// <summary>
/// 测试用的 <see cref="ILogger{TCategoryName}"/> 实现，收集每条日志的级别、格式化消息、结构化字段与异常。
/// </summary>
/// <typeparam name="TCategory">日志类别类型。</typeparam>
internal sealed class FakeLogger<TCategory> : ILogger<TCategory>
{
    private readonly List<FakeLogEntry> _entries = [];

    /// <summary>
    /// 按写入顺序返回已收集的全部日志条目。
    /// </summary>
    public IReadOnlyList<FakeLogEntry> Entries => _entries;

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
        => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        Dictionary<string, object?> values = new(StringComparer.Ordinal);
        if (state is IReadOnlyList<KeyValuePair<string, object?>> kvps)
        {
            foreach (KeyValuePair<string, object?> kvp in kvps)
            {
                values[kvp.Key] = kvp.Value;
            }
        }

        _entries.Add(new FakeLogEntry(
            logLevel,
            formatter(state, exception),
            values,
            exception));
    }
}

/// <summary>
/// 单条日志条目的只读快照。
/// </summary>
internal sealed record FakeLogEntry(
    LogLevel Level,
    string Message,
    IReadOnlyDictionary<string, object?> State,
    Exception? Exception);
