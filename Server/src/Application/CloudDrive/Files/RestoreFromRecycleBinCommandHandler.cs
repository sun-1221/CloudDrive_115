using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.Commands;

using Domain.Enums;
using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Files;

internal sealed class RestoreFromRecycleBinCommandHandler(
    ICloudDriveJobOrchestrator jobOrchestrator,
    ICloudDriveAccountContextAccessor accountContextAccessor,
    IOptions<CloudDriveAppConfig> configOptions)
    : ICommandHandler<RestoreFromRecycleBinCommand, CloudDriveOperationResultDto>
{
    public async Task<Result<CloudDriveOperationResultDto>> Handle(
        RestoreFromRecycleBinCommand request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        string cloudAccountUid = accountContextAccessor.CloudAccountUid;

        if (CloudDriveFileHandlerSupport.ShouldRunBatchAsync(request.Input.RecycleBinItemIds.Count))
        {
            Guid jobId = await jobOrchestrator.EnqueueBatchOperationAsync(
                request.UserId,
                cloudAccountUid,
                CloudDriveJobType.BatchRecycleBinRestore,
                request.Input,
                cancellationToken);

            return Result.Success(CloudDriveFileHandlerSupport.CreateAcceptedResult(jobId, "回收站还原任务已受理。"));
        }

        return Result.Failure<CloudDriveOperationResultDto>(
            CloudDriveFileHandlerSupport.ApiNotCollected("回收站还原"));
    }
}
