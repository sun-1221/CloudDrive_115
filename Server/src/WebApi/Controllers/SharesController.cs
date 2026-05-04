using Domain.Results;

using Application.CloudDrive.Dtos;
using Application.CloudDrive.Share;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using WebApi.Extensions;

namespace WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/shares")]
public sealed class SharesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IResult> GetAsync(
        [FromQuery] string tab,
        [FromQuery] int offset = 0,
        [FromQuery] int limit = 30,
        CancellationToken cancellationToken = default)
    {
        var input = new GetShareListInput
        {
            Offset = offset,
            Limit = limit,
        };

        Guid userId = this.GetAuthenticatedUserId();
        return tab switch
        {
            "slist" => (await sender.Send(new GetMySharesQuery(userId, input), cancellationToken)).ToHttpResult(),
            "send" => (await sender.Send(new GetShareSendsQuery(userId, input), cancellationToken)).ToHttpResult(),
            "receive" => (await sender.Send(new GetShareReceivesQuery(userId, input), cancellationToken)).ToHttpResult(),
            _ => Result.Failure(new Error(
                "cloud_drive_share_tab_invalid",
                "tab 必须为 slist、send 或 receive。")).ToHttpResult(),
        };
    }
}
