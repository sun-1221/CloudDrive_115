using Application.CloudDrive.Dtos;
using Application.CloudDrive.Video;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using WebApi.Extensions;
using WebApi.Models;

namespace WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/videos")]
public sealed class VideosController(ISender sender) : ControllerBase
{
    [HttpGet("{pickCode}/play")]
    public async Task<IResult> PlayAsync(
        string pickCode,
        [FromQuery] int? definition = null,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetVideoPlayUrlCommand(
                this.GetAuthenticatedUserId(),
                new GetVideoPlayInput
                {
                    PickCode = pickCode,
                    Definition = definition,
                }),
            cancellationToken);

        return result.ToOperationHttpResult(dto => dto.JobId.HasValue);
    }

    [HttpPost("{pickCode}/transcode")]
    public async Task<IResult> TranscodeAsync(string pickCode, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new SubmitTranscodeCommand(
                this.GetAuthenticatedUserId(),
                new SubmitTranscodeInput { PickCode = pickCode }),
            cancellationToken);

        return result.ToOperationHttpResult(dto => dto.JobId.HasValue);
    }

    [HttpGet("{pickCode}/subtitles")]
    public async Task<IResult> GetSubtitlesAsync(string pickCode, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetSubtitlesQuery(
                this.GetAuthenticatedUserId(),
                new GetSubtitlesInput { PickCode = pickCode }),
            cancellationToken);

        return result.ToHttpResult();
    }

    [HttpGet("{pickCode}/history")]
    public async Task<IResult> GetHistoryAsync(string pickCode, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetVideoPlayProgressQuery(
                this.GetAuthenticatedUserId(),
                new GetVideoPlayProgressInput { PickCode = pickCode }),
            cancellationToken);

        return result.ToHttpResult();
    }

    [HttpPost("{pickCode}/history")]
    public async Task<IResult> RememberHistoryAsync(
        string pickCode,
        [FromBody] VideoProgressRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new RememberVideoPlayProgressCommand(
                this.GetAuthenticatedUserId(),
                new RememberVideoPlayProgressInput
                {
                    PickCode = pickCode,
                    Time = request.Time,
                    WatchEnd = request.WatchEnd,
                }),
            cancellationToken);

        return result.ToHttpResult();
    }
}
