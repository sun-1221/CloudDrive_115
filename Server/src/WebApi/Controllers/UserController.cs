using Domain.Results;

using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.CloudDrive.User;

using MediatR;

using Microsoft.AspNetCore.Mvc;

using WebApi.Extensions;

namespace WebApi.Controllers;

[ApiController]
[Route("api/user")]
public sealed class UserController(ISender sender) : ControllerBase
{
    [HttpGet("profile")]
    public Task<IResult> GetProfileAsync(CancellationToken cancellationToken) => GetInfoAsync(cancellationToken);

    [HttpGet("info")]
    public async Task<IResult> GetInfoAsync(CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetUserInfoQuery(this.GetAuthenticatedUserId(), new CloudDriveAccountScopedInput()),
            cancellationToken);

        return result.ToHttpResult();
    }

    [HttpGet("vip")]
    public async Task<IResult> GetVipAsync(CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetUserVipQuery(this.GetAuthenticatedUserId(), new CloudDriveAccountScopedInput()),
            cancellationToken);

        return result.ToHttpResult();
    }

    [HttpGet("balance")]
    public IResult GetBalance()
    {
        return Result.Failure(new Error(
            "cloud_drive_user_balance_unavailable",
            "余额暂未作为独立 DTO 暴露，请使用 /api/user/profile 获取聚合信息。")).ToHttpResult();
    }

    [HttpGet("storage")]
    public async Task<IResult> GetStorageAsync(CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetUserSpaceQuery(this.GetAuthenticatedUserId(), new CloudDriveAccountScopedInput()),
            cancellationToken);

        return result.ToHttpResult();
    }
}
