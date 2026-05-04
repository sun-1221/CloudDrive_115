using Application.Queries;

namespace Architecture.Tests;

public sealed class QueryHandlerSideEffectTests
{
    [Fact]
    public void QueryHandlers_ShouldNotDependOn_ISender()
    {
        // QueryHandler 不应注入 ISender（用于分发 Command），这是写操作的入口。
        TestResult result = Types.InAssembly(ArchitectureConstants.ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(IQueryHandler<,>))
            .Should()
            .NotHaveDependencyOnAny("MediatR.ISender")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailures("CD0007: QueryHandler 依赖了 ISender", result));
    }

    [Fact]
    public void QueryHandlers_ShouldNotDependOn_IPublisher()
    {
        // QueryHandler 不应注入 IPublisher（用于发布事件/通知）。
        TestResult result = Types.InAssembly(ArchitectureConstants.ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(IQueryHandler<,>))
            .Should()
            .NotHaveDependencyOnAny("MediatR.IPublisher")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailures("CD0007: QueryHandler 依赖了 IPublisher", result));
    }

    private static string FormatFailures(string context, TestResult result)
    {
        if (result.IsSuccessful || result.FailingTypes is null)
        {
            return string.Empty;
        }

        return $"{context}:\n{string.Join("\n", result.FailingTypes.Select(t => $"  - {t.FullName}"))}";
    }
}
