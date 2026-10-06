using System.Reflection;

using Application.Commands;
using Application.Queries;

namespace Architecture.Tests;

/// <summary>
/// 应用层与映射扩展命名约束测试。
/// </summary>
public sealed class NamingConventionTests
{
    /// <summary>
    /// 验证所有 Command 类型名称以 Command 结尾。
    /// </summary>
    [Fact]
    public void Commands_ShouldEndWith_Command()
    {
        // Arrange（准备）

        // Act（执行）
        TestResult result = Types.InAssembly(ArchitectureConstants.ApplicationAssembly)
            .That()
            .ImplementInterface<ICommand>()
            .Or()
            .ImplementInterface(typeof(ICommand<>))
            .Should()
            .HaveNameEndingWith("Command")
            .GetResult();

        // Assert（断言）
        Assert.True(result.IsSuccessful, FormatFailures("CD0005: Command 命名不以 Command 结尾", result));
    }

    /// <summary>
    /// 验证所有 Query 类型名称以 Query 结尾。
    /// </summary>
    [Fact]
    public void Queries_ShouldEndWith_Query()
    {
        // Arrange（准备）

        // Act（执行）
        TestResult result = Types.InAssembly(ArchitectureConstants.ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(IQuery<>))
            .Should()
            .HaveNameEndingWith("Query")
            .GetResult();

        // Assert（断言）
        Assert.True(result.IsSuccessful, FormatFailures("CD0005: Query 命名不以 Query 结尾", result));
    }

    /// <summary>
    /// 验证所有 Query 类型名称以 Get 开头并以 Query 结尾。
    /// </summary>
    [Fact]
    public void Queries_ShouldStartWith_GetAndEndWith_Query()
    {
        // Arrange（准备）

        // Act（执行）
        List<string> violations = ArchitectureConstants.ApplicationAssembly
            .GetTypes()
            .Where(type => !type.IsAbstract && ImplementsOpenGenericInterface(type, typeof(IQuery<>)))
            .Where(type => !type.Name.StartsWith("Get", StringComparison.Ordinal)
                || !type.Name.EndsWith("Query", StringComparison.Ordinal))
            .Select(type => type.FullName ?? type.Name)
            .ToList();

        // Assert（断言）
        Assert.True(
            violations.Count == 0,
            $"CD0005: Query 命名必须符合 Get{{名词}}{{修饰}}Query:\n{string.Join("\n", violations.Select(v => $"  - {v}"))}");
    }

    /// <summary>
    /// 验证所有 CommandHandler 类型名称以 CommandHandler 结尾。
    /// </summary>
    [Fact]
    public void CommandHandlers_ShouldEndWith_CommandHandler()
    {
        // Arrange（准备）

        // Act（执行）
        TestResult result = Types.InAssembly(ArchitectureConstants.ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(ICommandHandler<>))
            .Or()
            .ImplementInterface(typeof(ICommandHandler<,>))
            .Should()
            .HaveNameEndingWith("CommandHandler")
            .GetResult();

        // Assert（断言）
        Assert.True(result.IsSuccessful, FormatFailures("CD0005: CommandHandler 命名不以 CommandHandler 结尾", result));
    }

    /// <summary>
    /// 验证所有 QueryHandler 类型名称以 QueryHandler 结尾。
    /// </summary>
    [Fact]
    public void QueryHandlers_ShouldEndWith_QueryHandler()
    {
        // Arrange（准备）

        // Act（执行）
        TestResult result = Types.InAssembly(ArchitectureConstants.ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(IQueryHandler<,>))
            .Should()
            .HaveNameEndingWith("QueryHandler")
            .GetResult();

        // Assert（断言）
        Assert.True(result.IsSuccessful, FormatFailures("CD0005: QueryHandler 命名不以 QueryHandler 结尾", result));
    }

    /// <summary>
    /// 验证 IServiceCollection 扩展方法名称以 Add 开头。
    /// </summary>
    [Fact]
    public void ServiceCollectionExtensions_ShouldStartWith_Add()
    {
        // Arrange（准备）
        // CD0009: IServiceCollection 扩展方法必须以 Add 开头。
        // 通过反射检查所有公开静态扩展方法。
        var violations = new List<string>();

        Type[] extensionTypes = ArchitectureConstants.InfrastructureAssembly.GetTypes()
            .Where(t => t.IsClass && t.IsSealed && t.IsAbstract) // static classes
            .Where(t => t.Name.Contains("Extensions") || t.Name.Contains("Setup"))
            .ToArray();

        // Act（执行）
        foreach (Type type in extensionTypes)
        {
            var extensionMethods = type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .Where(m => m.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false))
                .Where(m => m.GetParameters().Length > 0
                    && m.GetParameters()[0].ParameterType.Name == "IServiceCollection")
                .ToArray();

            foreach (var method in extensionMethods)
            {
                if (!method.Name.StartsWith("Add", StringComparison.Ordinal))
                {
                    violations.Add($"{type.FullName}.{method.Name}");
                }
            }
        }

        // Assert（断言）
        Assert.True(
            violations.Count == 0,
            $"CD0009: IServiceCollection 扩展方法未以 Add 开头:\n{string.Join("\n", violations.Select(v => $"  - {v}"))}");
    }

    /// <summary>
    /// 验证映射扩展方法统一命名为 ToDto 或 ToEntity。
    /// </summary>
    [Fact]
    public void MappingExtensionMethods_ShouldBeNamed_ToDtoOrToEntity()
    {
        // Arrange（准备）
        Assembly[] assemblies =
        [
            ArchitectureConstants.ApplicationAssembly,
            ArchitectureConstants.InfrastructureAssembly,
            ArchitectureConstants.WebApiAssembly,
        ];

        // Act（执行）
        List<string> violations = assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsClass && type.IsSealed && type.IsAbstract)
            .Where(type => type.Name.Contains("Mapping", StringComparison.Ordinal))
            .SelectMany(type => type
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .Where(method => method.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false))
                .Select(method => new { Type = type, Method = method }))
            .Where(item => item.Method.Name is not ("ToDto" or "ToEntity"))
            .Select(item => $"{item.Type.FullName}.{item.Method.Name}")
            .ToList();

        // Assert（断言）
        Assert.True(
            violations.Count == 0,
            $"CD0008: 映射扩展方法必须命名为 ToDto 或 ToEntity:\n{string.Join("\n", violations.Select(v => $"  - {v}"))}");
    }

    private static string FormatFailures(string context, TestResult result)
    {
        if (result.IsSuccessful || result.FailingTypes is null)
        {
            return string.Empty;
        }

        return $"{context}:\n{string.Join("\n", result.FailingTypes.Select(t => $"  - {t.FullName}"))}";
    }

    private static bool ImplementsOpenGenericInterface(Type type, Type openGenericInterface)
    {
        return type.GetInterfaces().Any(interfaceType =>
            interfaceType.IsGenericType
            && interfaceType.GetGenericTypeDefinition() == openGenericInterface);
    }
}
