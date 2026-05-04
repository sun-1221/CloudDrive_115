using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using WebApi.Extensions;

namespace WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/auth")]
public sealed class AuthController(ISender sender) : ControllerBase
{
    [HttpGet("check")]
    [AllowAnonymous]
    public async Task<IResult> CheckAsync(CancellationToken cancellationToken)
    {
        Guid? userId = this.User.Identity?.IsAuthenticated == true
            ? this.GetAuthenticatedUserId()
            : null;

        var result = await sender.Send(
            new GetLoginStatusQuery(userId),
            cancellationToken);

        return result.ToHttpResult();
    }
}
