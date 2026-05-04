using Application.Commands;
using Application.Queries;

namespace Architecture.Tests;

public sealed class NamingConventionTests
{
    [Fact]
    public void Commands_ShouldEndWith_Command()
    {
        TestResult result = Types.InAssembly(ArchitectureConstants.ApplicationAssembly)
            .That()
            .ImplementInterface<ICommand>()
            .Or()
            .ImplementInterface(typeof(ICommand<>))
            .Should()
            .HaveNameEndingWith("Command")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailures("CD0005: Command 命名不以 Command 结尾", result));
    }

    [Fact]
    public void Queries_ShouldEndWith_Query()
    {
        TestResult result = Types.InAssembly(ArchitectureConstants.ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(IQuery<>))
            .Should()
            .HaveNameEndingWith("Query")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailures("CD0005: Query 命名不以 Query 结尾", result));
    }

    [Fact]
    public void CommandHandlers_ShouldEndWith_CommandHandler()
    {
        TestResult result = Types.InAssembly(ArchitectureConstants.ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(ICommandHandler<>))
            .Or()
            .ImplementInterface(typeof(ICommandHandler<,>))
            .Should()
            .HaveNameEndingWith("CommandHandler")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailures("CD0005: CommandHandler 命名不以 CommandHandler 结尾", result));
    }

    [Fact]
    public void QueryHandlers_ShouldEndWith_QueryHandler()
    {
        TestResult result = Types.InAssembly(ArchitectureConstants.ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(IQueryHandler<,>))
            .Should()
            .HaveNameEndingWith("QueryHandler")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailures("CD0005: QueryHandler 命名不以 QueryHandler 结尾", result));
    }

    [Fact]
    public void ServiceCollectionExtensions_ShouldStartWith_Add()
    {
        // CD0009: IServiceCollection 扩展方法必须以 Add 开头。
        // 通过反射检查所有公开静态扩展方法。
        var violations = new List<string>();

        Type[] extensionTypes = ArchitectureConstants.InfrastructureAssembly.GetTypes()
            .Where(t => t.IsClass && t.IsSealed && t.IsAbstract) // static classes
            .Where(t => t.Name.Contains("Extensions") || t.Name.Contains("Setup"))
            .ToArray();

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

        Assert.True(
            violations.Count == 0,
            $"CD0009: IServiceCollection 扩展方法未以 Add 开头:\n{string.Join("\n", violations.Select(v => $"  - {v}"))}");
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
