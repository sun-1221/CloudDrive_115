using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using WebApi.Extensions;

namespace WebApi.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/cloud-drive/cookie-sessions")]
public sealed class CloudDriveAuthAdminController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IResult> UpsertAsync(
        [FromBody] UpsertCookieSessionInput input,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new UpsertCookieSessionCommand(this.GetAuthenticatedUserId(), input),
            cancellationToken);

        return result.ToCreatedHttpResult();
    }

    [HttpPost("{cloudAccountUid}/cooldown")]
    public async Task<IResult> EnterCooldownAsync(
        string cloudAccountUid,
        [FromBody] EnterCookieCooldownInput input,
        CancellationToken cancellationToken)
    {
        input.CloudAccountUid = cloudAccountUid;
        var result = await sender.Send(
            new EnterCookieCooldownCommand(this.GetAuthenticatedUserId(), input),
            cancellationToken);

        return result.ToHttpResult();
    }

    [HttpDelete("{cloudAccountUid}")]
    public async Task<IResult> RevokeAsync(
        string cloudAccountUid,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new RevokeCookieSessionCommand(
                this.GetAuthenticatedUserId(),
                new RevokeCookieSessionInput { CloudAccountUid = cloudAccountUid }),
            cancellationToken);

        return result.ToHttpResult();
    }
}
