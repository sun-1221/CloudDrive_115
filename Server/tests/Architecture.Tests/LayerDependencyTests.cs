namespace Architecture.Tests;

/// <summary>
/// CD0001/CD0002: 层依赖方向测试 — Domain 和 Application 禁止反向依赖基础设施和 WebApi 层。
/// </summary>
public sealed class LayerDependencyTests
{
    /// <summary>
    /// 验证 Domain 层不依赖 Application 层。
    /// </summary>
    [Fact]
    public void Domain_ShouldNotReference_Application()
    {
        // Arrange（准备）

        // Act（执行）
        TestResult result = Types.InAssembly(ArchitectureConstants.DomainAssembly)
            .That()
            .ResideInNamespace(ArchitectureConstants.DomainNamespace)
            .Should()
            .NotHaveDependencyOnAny(ArchitectureConstants.ApplicationNamespace)
            .GetResult();

        // Assert（断言）
        Assert.True(result.IsSuccessful, FormatFailures(result));
    }

    /// <summary>
    /// 验证 Domain 层不依赖 Infrastructure 层。
    /// </summary>
    [Fact]
    public void Domain_ShouldNotReference_Infrastructure()
    {
        // Arrange（准备）

        // Act（执行）
        TestResult result = Types.InAssembly(ArchitectureConstants.DomainAssembly)
            .That()
            .ResideInNamespace(ArchitectureConstants.DomainNamespace)
            .Should()
            .NotHaveDependencyOnAny(ArchitectureConstants.InfrastructureNamespace)
            .GetResult();

        // Assert（断言）
        Assert.True(result.IsSuccessful, FormatFailures(result));
    }

    /// <summary>
    /// 验证 Domain 层不依赖 WebApi 层。
    /// </summary>
    [Fact]
    public void Domain_ShouldNotReference_WebApi()
    {
        // Arrange（准备）

        // Act（执行）
        TestResult result = Types.InAssembly(ArchitectureConstants.DomainAssembly)
            .That()
            .ResideInNamespace(ArchitectureConstants.DomainNamespace)
            .Should()
            .NotHaveDependencyOnAny(ArchitectureConstants.WebApiNamespace)
            .GetResult();

        // Assert（断言）
        Assert.True(result.IsSuccessful, FormatFailures(result));
    }

    /// <summary>
    /// 验证 Application 层不依赖 Infrastructure 层。
    /// </summary>
    [Fact]
    public void Application_ShouldNotReference_Infrastructure()
    {
        // Arrange（准备）

        // Act（执行）
        TestResult result = Types.InAssembly(ArchitectureConstants.ApplicationAssembly)
            .That()
            .ResideInNamespace(ArchitectureConstants.ApplicationNamespace)
            .Should()
            .NotHaveDependencyOnAny(ArchitectureConstants.InfrastructureNamespace)
            .GetResult();

        // Assert（断言）
        Assert.True(result.IsSuccessful, FormatFailures(result));
    }

    /// <summary>
    /// 验证 Application 层不依赖 WebApi 层。
    /// </summary>
    [Fact]
    public void Application_ShouldNotReference_WebApi()
    {
        // Arrange（准备）

        // Act（执行）
        TestResult result = Types.InAssembly(ArchitectureConstants.ApplicationAssembly)
            .That()
            .ResideInNamespace(ArchitectureConstants.ApplicationNamespace)
            .Should()
            .NotHaveDependencyOnAny(ArchitectureConstants.WebApiNamespace)
            .GetResult();

        // Assert（断言）
        Assert.True(result.IsSuccessful, FormatFailures(result));
    }

    private static string FormatFailures(TestResult result)
    {
        if (result.IsSuccessful || result.FailingTypes is null)
        {
            return string.Empty;
        }

        return $"违规类型:\n{string.Join("\n", result.FailingTypes.Select(t => $"  - {t.FullName}"))}";
    }
}
