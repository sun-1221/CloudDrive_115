using System.Reflection;

using Application.CloudDrive.Dtos;

using MediatR;

using Microsoft.AspNetCore.Mvc;

namespace Architecture.Tests;

public sealed class CloudDriveControllerContractTests
{
    [Fact]
    public void Controllers_ShouldInjectOnly_ISender()
    {
        Type[] controllers = ArchitectureConstants.WebApiAssembly
            .GetTypes()
            .Where(type => typeof(ControllerBase).IsAssignableFrom(type)
                && !type.IsAbstract)
            .ToArray();

        foreach (Type controller in controllers)
        {
            ConstructorInfo constructor = Assert.Single(controller.GetConstructors());
            Type[] parameterTypes = constructor.GetParameters()
                .Select(parameter => parameter.ParameterType)
                .ToArray();

            Assert.All(parameterTypes, type => Assert.Equal(typeof(ISender), type));
        }
    }

    [Fact]
    public void PublicControllerActions_ShouldNotExpose_CloudAccountUidParameter()
    {
        Type[] publicControllers = ArchitectureConstants.WebApiAssembly
            .GetTypes()
            .Where(type => typeof(ControllerBase).IsAssignableFrom(type)
                && !type.IsAbstract
                && type.Name != "CloudDriveAuthAdminController")
            .ToArray();

        foreach (Type controller in publicControllers)
        {
            IEnumerable<ParameterInfo> parameters = controller
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .SelectMany(method => method.GetParameters());

            Assert.DoesNotContain(parameters, parameter =>
                string.Equals(parameter.Name, "cloudAccountUid", StringComparison.OrdinalIgnoreCase)
                || parameter.ParameterType == typeof(CloudDriveAccountScopedInput)
                || typeof(CloudDriveAccountScopedInput).IsAssignableFrom(parameter.ParameterType));
        }
    }

    [Fact]
    public void PublicWebApiRequestModels_ShouldNotExpose_CloudAccountUidProperty()
    {
        Type[] requestModels = ArchitectureConstants.WebApiAssembly
            .GetTypes()
            .Where(type => type.Namespace == "WebApi.Models")
            .ToArray();

        foreach (Type model in requestModels)
        {
            Assert.DoesNotContain(
                model.GetProperties(BindingFlags.Instance | BindingFlags.Public),
                property => string.Equals(property.Name, "CloudAccountUid", StringComparison.OrdinalIgnoreCase));
        }
    }
}
