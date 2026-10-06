using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using WebApi.Extensions;

namespace WebApi.Controllers;

/// <summary>兼容导入已有 115 Cookie 的个人账号接口。</summary>
[ApiController]
[Authorize]
[Route("api/cloud-drive/accounts/import")]
public sealed class CloudDriveAccountImportController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IResult> UpsertAsync([FromBody] UpsertCookieSessionInput input, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpsertCookieSessionCommand(this.GetAuthenticatedUserId(), input), cancellationToken);
        return result.ToCreatedHttpResult();
    }
}
