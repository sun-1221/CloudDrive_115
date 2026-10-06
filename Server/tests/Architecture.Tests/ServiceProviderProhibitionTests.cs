namespace Architecture.Tests;

/// <summary>
/// CD0002 补充：Application 层类型禁止直接依赖 <see cref="IServiceProvider"/>，
/// 避免服务定位器反模式，所有依赖必须通过构造函数显式注入。
/// </summary>
public sealed class ServiceProviderProhibitionTests
{
    [Fact]
    public void Application_ShouldNotDependOn_IServiceProvider()
    {
        // Arrange（准备）
        // NetArchTest 的 HaveDependencyOn("System") 范围过广，改用反射精确检测
        IEnumerable<Type> applicationTypes = ArchitectureConstants.ApplicationAssembly
            .GetTypes()
            .Where(t => t.Namespace is not null && t.Namespace.StartsWith(
                ArchitectureConstants.ApplicationNamespace, StringComparison.Ordinal));

        List<string> violations = [];

        // Act（执行）
        foreach (Type type in applicationTypes)
        {
            // 检查构造函数参数
            foreach (System.Reflection.ConstructorInfo ctor in type.GetConstructors(
                System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Instance))
            {
                foreach (System.Reflection.ParameterInfo param in ctor.GetParameters())
                {
                    if (param.ParameterType == typeof(IServiceProvider))
                    {
                        violations.Add($"{type.FullName} 构造函数参数 '{param.Name}' 注入了 IServiceProvider");
                    }
                }
            }

            // 检查字段
            foreach (System.Reflection.FieldInfo field in type.GetFields(
                System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.Static))
            {
                if (field.FieldType == typeof(IServiceProvider))
                {
                    violations.Add($"{type.FullName} 字段 '{field.Name}' 依赖了 IServiceProvider");
                }
            }
        }

        // Assert（断言）
        Assert.Empty(violations);
    }
}
