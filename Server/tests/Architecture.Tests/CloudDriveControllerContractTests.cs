using System.Reflection;

using Application.CloudDrive.Dtos;

using MediatR;

using Microsoft.AspNetCore.Mvc;

namespace Architecture.Tests;

/// <summary>
/// 115 网盘控制器契约架构测试。
/// </summary>
public sealed class CloudDriveControllerContractTests
{
    /// <summary>
    /// 验证控制器构造函数仅注入 ISender。
    /// </summary>
    [Fact]
    public void Controllers_ShouldInjectOnly_ISender()
    {
        // Arrange（准备）
        Type[] controllers = ArchitectureConstants.WebApiAssembly
            .GetTypes()
            .Where(type => typeof(ControllerBase).IsAssignableFrom(type)
                && !type.IsAbstract)
            .ToArray();

        // Act（执行）
        foreach (Type controller in controllers)
        {
            ConstructorInfo constructor = Assert.Single(controller.GetConstructors());
            Type[] parameterTypes = constructor.GetParameters()
                .Select(parameter => parameter.ParameterType)
                .ToArray();

            // Assert（断言）
            Assert.All(parameterTypes, type => Assert.Equal(typeof(ISender), type));
        }
    }

    /// <summary>
    /// 验证公开控制器动作不暴露 CloudAccountUid 参数。
    /// </summary>
    [Fact]
    public void PublicControllerActions_ShouldNotExpose_CloudAccountUidParameter()
    {
        // Arrange（准备）
        Type[] publicControllers = ArchitectureConstants.WebApiAssembly
            .GetTypes()
            .Where(type => typeof(ControllerBase).IsAssignableFrom(type)
                && !type.IsAbstract
                && type.Name != "CloudDriveAuthAdminController")
            .ToArray();

        // Act（执行）
        foreach (Type controller in publicControllers)
        {
            IEnumerable<ParameterInfo> parameters = controller
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .SelectMany(method => method.GetParameters());

            // Assert（断言）
            Assert.DoesNotContain(parameters, parameter =>
                string.Equals(parameter.Name, "cloudAccountUid", StringComparison.OrdinalIgnoreCase)
                || parameter.ParameterType == typeof(CloudDriveAccountScopedInput)
                || typeof(CloudDriveAccountScopedInput).IsAssignableFrom(parameter.ParameterType));
        }
    }

    /// <summary>
    /// 验证公开 WebApi 请求模型不暴露 CloudAccountUid 属性。
    /// </summary>
    [Fact]
    public void PublicWebApiRequestModels_ShouldNotExpose_CloudAccountUidProperty()
    {
        // Arrange（准备）
        Type[] requestModels = ArchitectureConstants.WebApiAssembly
            .GetTypes()
            .Where(type => type.Namespace == "WebApi.Models")
            .ToArray();

        // Act（执行）
        foreach (Type model in requestModels)
        {
            // Assert（断言）
            Assert.DoesNotContain(
                model.GetProperties(BindingFlags.Instance | BindingFlags.Public),
                property => string.Equals(property.Name, "CloudAccountUid", StringComparison.OrdinalIgnoreCase));
        }
    }
}
