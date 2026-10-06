using Application.CloudDrive.Auth;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using WebApi.Extensions;

namespace WebApi.Controllers;

/// <summary>当前个人用户的 115 账号绑定与选择接口。</summary>
[ApiController]
[Authorize]
[Route("api/cloud-drive/accounts")]
public sealed class CloudDriveAccountsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IResult> GetAsync(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCloudDriveAccountsQuery(this.GetAuthenticatedUserId()), cancellationToken);
        return result.ToHttpResult();
    }

    [HttpPost]
    public async Task<IResult> BindAsync([FromBody] BindCloudDriveAccountInput input, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new BindCloudDriveAccountCommand(this.GetAuthenticatedUserId(), this.GetAuthenticatedSessionId(), input), cancellationToken);
        return result.ToHttpResult();
    }

    [HttpPost("select")]
    public async Task<IResult> SelectAsync([FromBody] SelectAccountRequest input, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SelectCloudDriveAccountCommand(this.GetAuthenticatedUserId(), this.GetAuthenticatedSessionId(), input.CloudAccountUid), cancellationToken);
        return result.ToHttpResult();
    }

    [HttpDelete("{cloudAccountUid}")]
    public async Task<IResult> UnbindAsync(string cloudAccountUid, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UnbindCloudDriveAccountCommand(this.GetAuthenticatedUserId(), this.GetAuthenticatedSessionId(), cloudAccountUid), cancellationToken);
        return result.ToHttpResult();
    }
}

public sealed class SelectAccountRequest
{
    public string CloudAccountUid { get; set; } = string.Empty;
}
