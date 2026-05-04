using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
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
    [HttpGet("profile")]
    public async Task<IResult> GetProfileAsync(CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetUserProfileQuery(this.GetAuthenticatedUserId(), new CloudDriveAccountScopedInput()),
            cancellationToken);

        return result.ToHttpResult();
    }

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
    public async Task<IResult> GetBalanceAsync(CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetUserBalanceQuery(this.GetAuthenticatedUserId(), new CloudDriveAccountScopedInput()),
            cancellationToken);

        return result.ToHttpResult();
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
