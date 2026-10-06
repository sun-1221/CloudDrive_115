using Application.CloudDrive.Dtos;
using Application.CloudDrive.Files;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using WebApi.Extensions;
using WebApi.Models;

namespace WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/recycle")]
public sealed class RecycleBinController(ISender sender) : ControllerBase
{
    /// <summary>
    /// 获取回收站文件列表。
    /// </summary>
    /// <param name="offset">分页偏移量。</param>
    /// <param name="limit">分页返回数量上限。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示异步操作结果的 <see cref="Task{TResult}"/>。</returns>
    [HttpGet]
    public async Task<IResult> GetListAsync(
        [FromQuery] int offset = 0,
        [FromQuery] int limit = 30,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetRecycleBinListQuery(
                this.GetAuthenticatedUserId(),
                new GetRecycleBinListInput { Offset = offset, Limit = limit }),
            cancellationToken);

        return result.ToHttpResult();
    }

    /// <summary>
    /// 还原回收站中的文件。
    /// </summary>
    /// <param name="request">HTTP 请求载荷。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示异步操作结果的 <see cref="Task{TResult}"/>。</returns>
    [HttpPost("restore")]
    public async Task<IResult> RestoreAsync(
        [FromBody] RecycleBinItemsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new RestoreFromRecycleBinCommand(
                this.GetAuthenticatedUserId(),
                new RecycleBinOperationInput { RecycleBinItemIds = request.RecycleBinItemIds }),
            cancellationToken);

        return result.ToOperationHttpResult(dto => dto.Accepted);
    }

    /// <summary>
    /// 彻底删除回收站中的指定文件。
    /// </summary>
    /// <param name="id">资源唯一标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示异步操作结果的 <see cref="Task{TResult}"/>。</returns>
    [HttpDelete("{id}")]
    public async Task<IResult> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new DeleteFromRecycleBinCommand(
                this.GetAuthenticatedUserId(),
                new RecycleBinOperationInput { RecycleBinItemIds = [id] }),
            cancellationToken);

        return result.ToOperationHttpResult(dto => dto.Accepted);
    }

    /// <summary>
    /// 清空回收站。
    /// </summary>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示异步操作结果的 <see cref="Task{TResult}"/>。</returns>
    [HttpDelete]
    public async Task<IResult> ClearAsync(CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new ClearRecycleBinCommand(this.GetAuthenticatedUserId(), new ClearRecycleBinInput()),
            cancellationToken);

        return result.ToOperationHttpResult(dto => dto.Accepted);
    }
}
