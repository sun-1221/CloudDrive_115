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
    /// <summary>
    /// 获取视频播放地址。
    /// </summary>
    /// <param name="pickCode">115 文件提取码。</param>
    /// <param name="definition">视频清晰度定义。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示异步操作结果的 <see cref="Task{TResult}"/>。</returns>
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

    /// <summary>
    /// 提交视频转码任务。
    /// </summary>
    /// <param name="pickCode">115 文件提取码。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示异步操作结果的 <see cref="Task{TResult}"/>。</returns>
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

    /// <summary>
    /// 获取视频字幕列表。
    /// </summary>
    /// <param name="pickCode">115 文件提取码。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示异步操作结果的 <see cref="Task{TResult}"/>。</returns>
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

    /// <summary>
    /// 获取视频播放进度。
    /// </summary>
    /// <param name="pickCode">115 文件提取码。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示异步操作结果的 <see cref="Task{TResult}"/>。</returns>
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

    /// <summary>
    /// 记录视频播放进度。
    /// </summary>
    /// <param name="pickCode">115 文件提取码。</param>
    /// <param name="request">HTTP 请求载荷。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示异步操作结果的 <see cref="Task{TResult}"/>。</returns>
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
