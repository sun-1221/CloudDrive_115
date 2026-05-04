using System.Reflection;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using WebApi.Controllers;

namespace WebApi.Tests.Controllers;

public sealed class CloudDriveControllerSecurityTests
{
    [Fact]
    public void CloudDriveAuthAdminController_IsRestrictedToAdminRole()
    {
        // Arrange
        AuthorizeAttribute attribute = Assert.Single(
            typeof(CloudDriveAuthAdminController).GetCustomAttributes<AuthorizeAttribute>());

        // Act & Assert
        Assert.Equal("Admin", attribute.Roles);
    }

    [Fact]
    public void PublicCloudDriveControllers_DoNotUseAdminCookieSessionRoute()
    {
        // Arrange
        Type[] publicControllers = typeof(AuthController).Assembly.GetTypes()
            .Where(type => typeof(ControllerBase).IsAssignableFrom(type)
                && !type.IsAbstract
                && type.Name != nameof(CloudDriveAuthAdminController))
            .ToArray();

        // Act & Assert
        foreach (Type controller in publicControllers)
        {
            RouteAttribute? route = controller.GetCustomAttribute<RouteAttribute>();
            Assert.NotNull(route);
            Assert.DoesNotContain("admin/cloud-drive/cookie-sessions", route.Template, StringComparison.OrdinalIgnoreCase);
        }
    }
}
