using Microsoft.AspNetCore.Mvc;

namespace Architecture.Tests;

public sealed class ControllerDependencyTests
{
    [Fact]
    public void Controllers_ShouldNotDependOn_Repositories()
    {
        TestResult result = Types.InAssembly(ArchitectureConstants.WebApiAssembly)
            .That()
            .Inherit<ControllerBase>()
            .Should()
            .NotHaveDependencyOnAny("Domain.Repositories")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailures("Controller 注入了仓储接口", result));
    }

    [Fact]
    public void Controllers_ShouldNotDependOn_SqlSugar()
    {
        TestResult result = Types.InAssembly(ArchitectureConstants.WebApiAssembly)
            .That()
            .Inherit<ControllerBase>()
            .Should()
            .NotHaveDependencyOnAny("SqlSugar")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailures("Controller 注入了 SqlSugar", result));
    }

    private static string FormatFailures(string context, TestResult result)
    {
        if (result.IsSuccessful || result.FailingTypes is null)
        {
            return string.Empty;
        }

        return $"CD0003: {context}:\n{string.Join("\n", result.FailingTypes.Select(t => $"  - {t.FullName}"))}";
    }
}
