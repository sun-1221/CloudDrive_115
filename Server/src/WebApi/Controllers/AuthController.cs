using Application.CloudDrive.Auth;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using WebApi.Extensions;

namespace WebApi.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(ISender sender, IJwtTokenFactory tokenFactory) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IResult> RegisterAsync([FromBody] RegisterInput input, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RegisterCommand(input), cancellationToken);
        return result.IsSuccess
            ? Results.Ok(new { accessToken = tokenFactory.Create(result.Value), tokenType = "Bearer", expiresAt = result.Value.ExpiresAt, userId = result.Value.UserId })
            : result.ToHttpResult();
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IResult> LoginAsync([FromBody] LoginInput input, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new LoginCommand(input), cancellationToken);
        return result.IsSuccess
            ? Results.Ok(new { accessToken = tokenFactory.Create(result.Value), tokenType = "Bearer", expiresAt = result.Value.ExpiresAt, userId = result.Value.UserId })
            : result.ToHttpResult();
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IResult> LogoutAsync(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new LogoutCommand(this.GetAuthenticatedUserId(), this.GetAuthenticatedSessionId()), cancellationToken);
        return result.ToHttpResult();
    }

    [HttpPost("refresh")]
    [Authorize]
    public async Task<IResult> RefreshAsync(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RefreshCommand(this.GetAuthenticatedUserId(), this.GetAuthenticatedSessionId()), cancellationToken);
        return result.IsSuccess
            ? Results.Ok(new { accessToken = tokenFactory.Create(result.Value), tokenType = "Bearer", expiresAt = result.Value.ExpiresAt, userId = result.Value.UserId })
            : result.ToHttpResult();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IResult> MeAsync(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCurrentUserQuery(this.GetAuthenticatedUserId(), this.GetAuthenticatedSessionId()), cancellationToken);
        return result.ToHttpResult();
    }

    /// <summary>
    /// 探测当前 115 Cookie 登录态（支持匿名访问）。
    /// </summary>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示异步操作结果的 <see cref="Task{TResult}"/>。</returns>
    [HttpGet("check")]
    [AllowAnonymous]
    public async Task<IResult> CheckAsync(CancellationToken cancellationToken)
    {
        Guid? userId = this.User.Identity?.IsAuthenticated == true
            ? this.GetAuthenticatedUserId()
            : null;
        Guid? sessionId = this.User.Identity?.IsAuthenticated == true
            ? this.GetAuthenticatedSessionId()
            : null;

        var result = await sender.Send(
            new GetLoginStatusQuery(userId, sessionId),
            cancellationToken);

        return result.ToHttpResult();
    }
}
