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
