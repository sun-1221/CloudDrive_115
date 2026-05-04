namespace Architecture.Tests;

/// <summary>
/// CD0001/CD0002: 层依赖方向测试 — Domain 和 Application 禁止反向依赖基础设施和 WebApi 层。
/// </summary>
public sealed class LayerDependencyTests
{
    [Fact]
    public void Domain_ShouldNotReference_Application()
    {
        TestResult result = Types.InAssembly(ArchitectureConstants.DomainAssembly)
            .That()
            .ResideInNamespace(ArchitectureConstants.DomainNamespace)
            .Should()
            .NotHaveDependencyOnAny(ArchitectureConstants.ApplicationNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailures(result));
    }

    [Fact]
    public void Domain_ShouldNotReference_Infrastructure()
    {
        TestResult result = Types.InAssembly(ArchitectureConstants.DomainAssembly)
            .That()
            .ResideInNamespace(ArchitectureConstants.DomainNamespace)
            .Should()
            .NotHaveDependencyOnAny(ArchitectureConstants.InfrastructureNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailures(result));
    }

    [Fact]
    public void Domain_ShouldNotReference_WebApi()
    {
        TestResult result = Types.InAssembly(ArchitectureConstants.DomainAssembly)
            .That()
            .ResideInNamespace(ArchitectureConstants.DomainNamespace)
            .Should()
            .NotHaveDependencyOnAny(ArchitectureConstants.WebApiNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailures(result));
    }

    [Fact]
    public void Application_ShouldNotReference_Infrastructure()
    {
        TestResult result = Types.InAssembly(ArchitectureConstants.ApplicationAssembly)
            .That()
            .ResideInNamespace(ArchitectureConstants.ApplicationNamespace)
            .Should()
            .NotHaveDependencyOnAny(ArchitectureConstants.InfrastructureNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailures(result));
    }

    [Fact]
    public void Application_ShouldNotReference_WebApi()
    {
        TestResult result = Types.InAssembly(ArchitectureConstants.ApplicationAssembly)
            .That()
            .ResideInNamespace(ArchitectureConstants.ApplicationNamespace)
            .Should()
            .NotHaveDependencyOnAny(ArchitectureConstants.WebApiNamespace)
            .GetResult();

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
