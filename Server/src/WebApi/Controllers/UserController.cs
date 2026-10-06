using Application.CloudDrive.Auth;
using Application.CloudDrive.User;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using WebApi.Extensions;

namespace WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/user")]
public sealed class UserController(ISender sender) : ControllerBase
{
    /// <summary>
    /// 获取当前用户的 115 个人资料。
    /// </summary>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示异步操作结果的 <see cref="Task{TResult}"/>。</returns>
    [HttpGet("profile")]
    public async Task<IResult> GetProfileAsync(CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetUserProfileQuery(this.GetAuthenticatedUserId()),
            cancellationToken);

        return result.ToHttpResult();
    }

    /// <summary>
    /// 获取当前用户的 115 基本信息。
    /// </summary>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示异步操作结果的 <see cref="Task{TResult}"/>。</returns>
    [HttpGet("info")]
    public async Task<IResult> GetInfoAsync(CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetUserInfoQuery(this.GetAuthenticatedUserId()),
            cancellationToken);

        return result.ToHttpResult();
    }

    /// <summary>
    /// 获取当前用户的 VIP 会员信息。
    /// </summary>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示异步操作结果的 <see cref="Task{TResult}"/>。</returns>
    [HttpGet("vip")]
    public async Task<IResult> GetVipAsync(CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetUserVipQuery(this.GetAuthenticatedUserId()),
            cancellationToken);

        return result.ToHttpResult();
    }

    /// <summary>
    /// 获取当前用户的余额信息。
    /// </summary>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示异步操作结果的 <see cref="Task{TResult}"/>。</returns>
    [HttpGet("balance")]
    public async Task<IResult> GetBalanceAsync(CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetUserBalanceQuery(this.GetAuthenticatedUserId()),
            cancellationToken);

        return result.ToHttpResult();
    }

    /// <summary>
    /// 获取当前用户的存储空间信息。
    /// </summary>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示异步操作结果的 <see cref="Task{TResult}"/>。</returns>
    [HttpGet("storage")]
    public async Task<IResult> GetStorageAsync(CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetUserSpaceQuery(this.GetAuthenticatedUserId()),
            cancellationToken);

        return result.ToHttpResult();
    }
}
