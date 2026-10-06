using System.Reflection;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using WebApi.Controllers;

namespace WebApi.Tests.Controllers;

/// <summary>
/// 115 网盘控制器安全约束测试。
/// </summary>
public sealed class CloudDriveControllerSecurityTests
{
    /// <summary>
    /// 验证账号导入接口不再依赖 Admin 角色。
    /// </summary>
    [Fact]
    public void CloudDriveAccountImportController_WhenInspected_DoesNotUseAdminRole()
    {
        // Arrange（准备）
        AuthorizeAttribute attribute = Assert.Single(
            typeof(CloudDriveAccountImportController).GetCustomAttributes<AuthorizeAttribute>());

        // Act（执行）

        // Assert（断言）
        Assert.Null(attribute.Roles);
    }

    /// <summary>
    /// 验证公开控制器不会复用管理员 Cookie 会话路由。
    /// </summary>
    [Fact]
    public void PublicCloudDriveControllers_WhenInspected_DoNotUseAdminCookieSessionRoute()
    {
        // Arrange（准备）
        Type[] publicControllers = typeof(AuthController).Assembly.GetTypes()
            .Where(type => typeof(ControllerBase).IsAssignableFrom(type)
                && !type.IsAbstract
                && type.Name != nameof(CloudDriveAccountImportController))
            .ToArray();

        // Act（执行）
        foreach (Type controller in publicControllers)
        {
            RouteAttribute? route = controller.GetCustomAttribute<RouteAttribute>();

            // Assert（断言）
            Assert.NotNull(route);
            Assert.DoesNotContain("admin/", route.Template, StringComparison.OrdinalIgnoreCase);
        }
    }
}
