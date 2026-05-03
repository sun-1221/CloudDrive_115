using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.CloudDrive.Mappings;
using Application.Queries;

using Domain.Entities;
using Domain.Repositories;
using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Jobs;

internal sealed class GetJobStatusQueryHandler(
    ICloudDriveJobRepository jobRepository,
    IOptions<CloudDriveAppConfig> configOptions)
    : IQueryHandler<GetJobStatusQuery, CloudDriveJobDto>
{
    public async Task<Result<CloudDriveJobDto>> Handle(
        GetJobStatusQuery request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        if (request.Input.JobId == Guid.Empty)
        {
            return Result.Failure<CloudDriveJobDto>(
                new Error("cloud_drive_job_id_required", "后台任务 ID 不能为空。"));
        }

        if (string.IsNullOrWhiteSpace(request.Input.CloudAccountUid))
        {
            return Result.Failure<CloudDriveJobDto>(
                new Error("cloud_drive_cloud_account_uid_required", "缺少 CloudAccountUid，请指定 115 账号。"));
        }

        CloudDriveJob? job = await jobRepository.GetByIdForUserAsync(
            request.UserId,
            request.Input.JobId,
            cancellationToken);

        if (job is null || !string.Equals(job.CloudAccountUid, request.Input.CloudAccountUid, StringComparison.Ordinal))
        {
            return Result.Failure<CloudDriveJobDto>(
                new Error("cloud_drive_job_not_found", "后台任务不存在或不属于当前账号上下文。"));
        }

        return Result.Success(job.ToDto());
    }
}
