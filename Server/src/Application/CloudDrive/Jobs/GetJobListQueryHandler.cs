using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.CloudDrive.Mappings;
using Application.Queries;

using Domain.Entities;
using Domain.Repositories;
using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Jobs;

internal sealed class GetJobListQueryHandler(
    ICloudDriveJobRepository jobRepository,
    IOptions<CloudDriveAppConfig> configOptions)
    : IQueryHandler<GetJobListQuery, PagedResultDto<CloudDriveJobDto>>
{
    public async Task<Result<PagedResultDto<CloudDriveJobDto>>> Handle(
        GetJobListQuery request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        if (request.Input.SkipCount < 0)
        {
            return Result.Failure<PagedResultDto<CloudDriveJobDto>>(
                new Error("cloud_drive_invalid_skip_count", "SkipCount 不能小于 0。"));
        }

        if (request.Input.MaxResultCount <= 0)
        {
            return Result.Failure<PagedResultDto<CloudDriveJobDto>>(
                new Error("cloud_drive_invalid_max_result_count", "MaxResultCount 必须大于 0。"));
        }

        IReadOnlyList<CloudDriveJob> userJobs = await jobRepository.GetByUserAsync(
            request.UserId,
            cancellationToken);

        IEnumerable<CloudDriveJob> query = userJobs;

        if (!string.IsNullOrWhiteSpace(request.Input.CloudAccountUid))
        {
            query = query.Where(job => string.Equals(
                job.CloudAccountUid,
                request.Input.CloudAccountUid,
                StringComparison.Ordinal));
        }

        if (request.Input.Status.HasValue)
        {
            query = query.Where(job => job.Status == request.Input.Status.Value);
        }

        List<CloudDriveJob> filtered = query
            .OrderByDescending(job => job.CreationTime)
            .ToList();

        List<CloudDriveJobDto> items = filtered
            .Skip(request.Input.SkipCount)
            .Take(request.Input.MaxResultCount)
            .Select(job => job.ToDto())
            .ToList();

        return Result.Success(new PagedResultDto<CloudDriveJobDto>
        {
            Items = items,
            TotalCount = filtered.Count,
            SkipCount = request.Input.SkipCount,
            MaxResultCount = request.Input.MaxResultCount,
        });
    }
}
