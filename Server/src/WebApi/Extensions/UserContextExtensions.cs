using System.Security.Claims;

using Microsoft.AspNetCore.Mvc;

namespace WebApi.Extensions;

/// <summary>
/// WebApi 用户上下文解析扩展。
/// </summary>
public static class UserContextExtensions
{
    /// <summary>
    /// 从当前 Controller 用户声明中解析系统用户 ID。
    /// </summary>
    public static Guid GetAuthenticatedUserId(this ControllerBase controller)
    {
        ArgumentNullException.ThrowIfNull(controller);
        return controller.User.GetAuthenticatedUserId();
    }

    /// <summary>
    /// 从声明集合中解析系统用户 ID。
    /// </summary>
    public static Guid GetAuthenticatedUserId(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        string? value = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub")
            ?? principal.FindFirstValue("user_id");

        if (principal.Identity?.IsAuthenticated != true
            || !Guid.TryParse(value, out Guid userId))
        {
            throw new UnauthorizedAccessException("当前请求缺少有效的认证用户 ID。");
        }

        return userId;
    }
}
