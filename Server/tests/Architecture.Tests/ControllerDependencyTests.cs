using Microsoft.AspNetCore.Mvc;

namespace Architecture.Tests;

/// <summary>
/// Controller 依赖边界架构测试。
/// </summary>
public sealed class ControllerDependencyTests
{
    /// <summary>
    /// 验证 Controller 不直接依赖领域仓储接口。
    /// </summary>
    [Fact]
    public void Controllers_ShouldNotDependOn_Repositories()
    {
        // Arrange（准备）

        // Act（执行）
        TestResult result = Types.InAssembly(ArchitectureConstants.WebApiAssembly)
            .That()
            .Inherit<ControllerBase>()
            .Should()
            .NotHaveDependencyOnAny("Domain.Repositories")
            .GetResult();

        // Assert（断言）
        Assert.True(result.IsSuccessful, FormatFailures("Controller 注入了仓储接口", result));
    }

    /// <summary>
    /// 验证 Controller 不直接依赖 SqlSugar。
    /// </summary>
    [Fact]
    public void Controllers_ShouldNotDependOn_SqlSugar()
    {
        // Arrange（准备）

        // Act（执行）
        TestResult result = Types.InAssembly(ArchitectureConstants.WebApiAssembly)
            .That()
            .Inherit<ControllerBase>()
            .Should()
            .NotHaveDependencyOnAny("SqlSugar")
            .GetResult();

        // Assert（断言）
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
