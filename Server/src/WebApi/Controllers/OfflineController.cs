using Domain.Results;

using Application.CloudDrive.Dtos;
using Application.CloudDrive.Offline;

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

    [HttpGet("quota")]
    public async Task<IResult> GetQuotaAsync(CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetOfflineQuotaQuery(this.GetAuthenticatedUserId(), new CloudDriveAccountScopedInput()),
            cancellationToken);

        return result.ToHttpResult();
    }

    [HttpGet("stats")]
    public async Task<IResult> GetStatsAsync(CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetOfflineStatsQuery(this.GetAuthenticatedUserId(), new CloudDriveAccountScopedInput()),
            cancellationToken);

        return result.ToHttpResult();
    }

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
                    Urls = request.Urls,
                    SavePath = request.SaveDirectoryId,
                }),
            cancellationToken);

        return result.ToOperationHttpResult(dto => dto.Accepted);
    }

    [HttpPost("tasks/bt")]
    public async Task<IResult> AddBtAsync(
        [FromBody] OfflineBtTaskRequest request,
        CancellationToken cancellationToken)
    {
        var input = new AddOfflineTaskInput
        {
            Urls = [$"magnet:?xt=urn:btih:{request.InfoHash}"],
            SavePath = request.SaveDirectoryId ?? request.SavePath,
        };

        var result = await sender.Send(
            new AddOfflineTaskCommand(this.GetAuthenticatedUserId(), input),
            cancellationToken);

        return result.ToOperationHttpResult(dto => dto.Accepted);
    }

    [HttpPost("parse-torrent")]
    public IResult ParseTorrent()
    {
        return Result.Failure(new Error(
            "cloud_drive_api_not_collected",
            "BT 种子解析接口尚未采集，待后续联调补齐。")).ToHttpResult();
    }

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
