using Application.CloudDrive.Dtos;
using Application.CloudDrive.Jobs;

using Domain.Enums;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using WebApi.Extensions;

namespace WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/jobs")]
public sealed class JobsController(ISender sender) : ControllerBase
{
    /// <summary>
    /// 获取指定后台任务的状态。
    /// </summary>
    /// <param name="jobId">后台任务唯一标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示异步操作结果的 <see cref="Task{TResult}"/>。</returns>
    [HttpGet("{jobId:guid}")]
    public async Task<IResult> GetAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetJobStatusQuery(
                this.GetAuthenticatedUserId(),
                new GetJobStatusInput { JobId = jobId }),
            cancellationToken);

        return result.ToHttpResult();
    }

    /// <summary>
    /// 获取后台任务列表。
    /// </summary>
    /// <param name="status">后台任务状态筛选条件。</param>
    /// <param name="skipCount">分页跳过记录数。</param>
    /// <param name="maxResultCount">分页返回记录数上限。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示异步操作结果的 <see cref="Task{TResult}"/>。</returns>
    [HttpGet]
    public async Task<IResult> GetListAsync(
        [FromQuery] CloudDriveJobStatus? status = null,
        [FromQuery(Name = "offset")] int skipCount = 0,
        [FromQuery(Name = "limit")] int maxResultCount = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetJobListQuery(
                this.GetAuthenticatedUserId(),
                new GetJobListInput
                {
                    Status = status,
                    SkipCount = skipCount,
                    MaxResultCount = maxResultCount,
                }),
            cancellationToken);

        return result.ToHttpResult();
    }
}
