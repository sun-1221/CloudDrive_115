using Application.Queries;

namespace Architecture.Tests;

/// <summary>
/// QueryHandler 只读约束架构测试。
/// </summary>
public sealed class QueryHandlerSideEffectTests
{
    private static readonly string[] ProhibitedWriteTokens =
    [
        ".Insert(",
        ".InsertAsync(",
        ".Update(",
        ".UpdateAsync(",
        ".Delete(",
        ".DeleteAsync(",
        ".Publish(",
        ".PublishAsync(",
    ];

    /// <summary>
    /// 验证 QueryHandler 不依赖 ISender 以避免触发写操作。
    /// </summary>
    [Fact]
    public void QueryHandlers_ShouldNotDependOn_ISender()
    {
        // Arrange（准备）
        // QueryHandler 不应注入 ISender（用于分发 Command），这是写操作的入口。
        // Act（执行）
        TestResult result = Types.InAssembly(ArchitectureConstants.ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(IQueryHandler<,>))
            .Should()
            .NotHaveDependencyOnAny("MediatR.ISender")
            .GetResult();

        // Assert（断言）
        Assert.True(result.IsSuccessful, FormatFailures("CD0007: QueryHandler 依赖了 ISender", result));
    }

    /// <summary>
    /// 验证 QueryHandler 不依赖 IPublisher 以避免发布事件或通知。
    /// </summary>
    [Fact]
    public void QueryHandlers_ShouldNotDependOn_IPublisher()
    {
        // Arrange（准备）
        // QueryHandler 不应注入 IPublisher（用于发布事件/通知）。
        // Act（执行）
        TestResult result = Types.InAssembly(ArchitectureConstants.ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(IQueryHandler<,>))
            .Should()
            .NotHaveDependencyOnAny("MediatR.IPublisher")
            .GetResult();

        // Assert（断言）
        Assert.True(result.IsSuccessful, FormatFailures("CD0007: QueryHandler 依赖了 IPublisher", result));
    }

    /// <summary>
    /// 验证 QueryHandler 源码中不直接调用写操作或发布操作。
    /// </summary>
    [Fact]
    public void QueryHandlers_ShouldNotCall_WriteOperations()
    {
        // Arrange（准备）
        string sourceDirectory = LocateApplicationSourceDirectory();
        List<string> violations = [];

        // Act（执行）
        foreach (string file in Directory.EnumerateFiles(sourceDirectory, "*QueryHandler.cs", SearchOption.AllDirectories))
        {
            string[] lines = File.ReadAllLines(file);
            for (int index = 0; index < lines.Length; index++)
            {
                string line = lines[index].Trim();
                if (line.StartsWith("//", StringComparison.Ordinal)
                    || line.StartsWith("///", StringComparison.Ordinal))
                {
                    continue;
                }

                foreach (string token in ProhibitedWriteTokens)
                {
                    if (line.Contains(token, StringComparison.Ordinal))
                    {
                        violations.Add($"{Path.GetRelativePath(sourceDirectory, file)}:{index + 1} 包含 {token}");
                    }
                }
            }
        }

        // Assert（断言）
        Assert.True(
            violations.Count == 0,
            $"CD0007: QueryHandler 禁止调用写操作或发布事件:\n{string.Join("\n", violations.Select(v => $"  - {v}"))}");
    }

    private static string FormatFailures(string context, TestResult result)
    {
        if (result.IsSuccessful || result.FailingTypes is null)
        {
            return string.Empty;
        }

        return $"{context}:\n{string.Join("\n", result.FailingTypes.Select(t => $"  - {t.FullName}"))}";
    }

    private static string LocateApplicationSourceDirectory()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null)
        {
            string candidate = Path.Combine(current.FullName, "src", "Application");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            candidate = Path.Combine(current.FullName, "Server", "src", "Application");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("无法定位 Server/src/Application 源码目录，不能执行 CD0007 源码扫描。");
    }
}
