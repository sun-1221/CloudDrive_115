using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.Commands;

using Domain.Enums;
using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Files;

internal sealed class CopyFilesCommandHandler(
    ICloudDriveJobOrchestrator jobOrchestrator,
    ICloudDriveAccountContextAccessor accountContextAccessor,
    IOptions<CloudDriveAppConfig> configOptions)
    : ICommandHandler<CopyFilesCommand, CloudDriveOperationResultDto>
{
    public async Task<Result<CloudDriveOperationResultDto>> Handle(
        CopyFilesCommand request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        string cloudAccountUid = accountContextAccessor.CloudAccountUid;

        if (CloudDriveFileHandlerSupport.ShouldRunBatchAsync(request.Input.FileIds.Count))
        {
            Guid jobId = await jobOrchestrator.EnqueueBatchOperationAsync(
                request.UserId,
                cloudAccountUid,
                CloudDriveJobType.BatchCopy,
                request.Input,
                cancellationToken);

            return Result.Success(CloudDriveFileHandlerSupport.CreateAcceptedResult(jobId, "复制任务已受理。"));
        }

        return Result.Failure<CloudDriveOperationResultDto>(
            CloudDriveFileHandlerSupport.ApiNotCollected("文件复制"));
    }
}
