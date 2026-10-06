using Application.CloudDrive.Dtos;
using Application.CloudDrive.Offline;

using Domain.Results;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using WebApi.Extensions;
using WebApi.Models;

namespace WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/offline")]
public sealed class OfflineController(ISender sender) : ControllerBase
{
    /// <summary>
    /// 获取离线下载任务列表。
    /// </summary>
    /// <param name="page">分页页码。</param>
    /// <param name="stat">离线任务状态筛选条件。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示异步操作结果的 <see cref="Task{TResult}"/>。</returns>
    [HttpGet("tasks")]
    public async Task<IResult> GetTasksAsync(
        [FromQuery] int page = 1,
        [FromQuery] int stat = 0,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetOfflineTaskListQuery(
                this.GetAuthenticatedUserId(),
                new GetOfflineTaskListInput { Page = page, Stat = stat }),
            cancellationToken);

        return result.ToHttpResult();
    }

    /// <summary>
    /// 获取离线下载配额。
    /// </summary>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示异步操作结果的 <see cref="Task{TResult}"/>。</returns>
    [HttpGet("quota")]
    public async Task<IResult> GetQuotaAsync(CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetOfflineQuotaQuery(this.GetAuthenticatedUserId()),
            cancellationToken);

        return result.ToHttpResult();
    }

    /// <summary>
    /// 获取离线下载统计信息。
    /// </summary>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示异步操作结果的 <see cref="Task{TResult}"/>。</returns>
    [HttpGet("stats")]
    public async Task<IResult> GetStatsAsync(CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetOfflineStatsQuery(this.GetAuthenticatedUserId()),
            cancellationToken);

        return result.ToHttpResult();
    }

    /// <summary>
    /// 添加 URL 离线下载任务。
    /// </summary>
    /// <param name="request">HTTP 请求载荷。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示异步操作结果的 <see cref="Task{TResult}"/>。</returns>
    [HttpPost("tasks/urls")]
    public async Task<IResult> AddUrlsAsync(
        [FromBody] OfflineUrlTasksRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new AddOfflineTaskCommand(
                this.GetAuthenticatedUserId(),
                new AddOfflineTaskInput
                {
                    TaskKind = "Url",
                    Urls = request.Urls,
                    SaveDirectoryId = request.SaveDirectoryId,
                }),
            cancellationToken);

        return result.ToOperationHttpResult(dto => dto.Accepted);
    }

    /// <summary>
    /// 添加 BT 磁力链离线下载任务。
    /// </summary>
    /// <param name="request">HTTP 请求载荷。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示异步操作结果的 <see cref="Task{TResult}"/>。</returns>
    [HttpPost("tasks/bt")]
    public async Task<IResult> AddBtAsync(
        [FromBody] OfflineBtTaskRequest request,
        CancellationToken cancellationToken)
    {
        var input = new AddOfflineTaskInput
        {
            TaskKind = "Bt",
            InfoHash = request.InfoHash,
            Urls = [$"magnet:?xt=urn:btih:{request.InfoHash}"],
            SaveDirectoryId = request.SaveDirectoryId,
            SavePath = request.SavePath,
        };

        var result = await sender.Send(
            new AddOfflineTaskCommand(this.GetAuthenticatedUserId(), input),
            cancellationToken);

        return result.ToOperationHttpResult(dto => dto.Accepted);
    }

    /// <summary>
    /// 解析 BT 种子。
    /// </summary>
    [HttpPost("parse-torrent")]
    public IResult ParseTorrent()
    {
        return Result.Failure(new Error(
            "cloud_drive_api_not_collected",
            "BT 种子解析接口尚未采集，待后续联调补齐。")).ToHttpResult();
    }

    /// <summary>
    /// 取消指定离线下载任务。
    /// </summary>
    /// <param name="infoHash">离线任务 InfoHash。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示异步操作结果的 <see cref="Task{TResult}"/>。</returns>
    [HttpDelete("tasks/{infoHash}")]
    public async Task<IResult> CancelAsync(string infoHash, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CancelOfflineTaskCommand(
                this.GetAuthenticatedUserId(),
                new CancelOfflineTaskInput { InfoHash = infoHash }),
            cancellationToken);

        return result.ToOperationHttpResult(dto => dto.Accepted);
    }

    /// <summary>
    /// 清空指定状态的离线下载任务。
    /// </summary>
    /// <param name="request">HTTP 请求载荷。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示异步操作结果的 <see cref="Task{TResult}"/>。</returns>
    [HttpPost("tasks:clear")]
    public async Task<IResult> ClearAsync(
        [FromBody] ClearOfflineTasksRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new ClearOfflineTasksCommand(
                this.GetAuthenticatedUserId(),
                new ClearOfflineTasksInput { Stat = request.Stat }),
            cancellationToken);

        return result.ToOperationHttpResult(dto => dto.Accepted);
    }
}
