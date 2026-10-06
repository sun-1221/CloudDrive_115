using Application.CloudDrive.Dtos;
using Application.CloudDrive.Share;

using Domain.Results;

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
    /// <summary>
    /// 获取分享列表（按 tab 区分：slist/send/receive）。
    /// </summary>
    /// <param name="tab">离线任务分类页签。</param>
    /// <param name="offset">分页偏移量。</param>
    /// <param name="limit">分页返回数量上限。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示异步操作结果的 <see cref="Task{TResult}"/>。</returns>
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
