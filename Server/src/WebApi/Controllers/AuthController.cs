using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;

using MediatR;

using Microsoft.AspNetCore.Mvc;

using WebApi.Extensions;

namespace WebApi.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(ISender sender) : ControllerBase
{
    [HttpGet("check")]
    public async Task<IResult> CheckAsync(CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetLoginStatusQuery(this.GetAuthenticatedUserId(), new CloudDriveAccountScopedInput()),
            cancellationToken);

        return result.IsFailure
            ? result.ToHttpResult()
            : Results.Ok(new
            {
                isAuthenticated = result.Value.IsLoggedIn == true,
                result.Value.FailureReason,
            });
    }
}
