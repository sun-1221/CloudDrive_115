using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.Commands;

using Domain.Enums;
using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Files;

internal sealed class DeleteFromRecycleBinCommandHandler(
    ICloudDriveJobOrchestrator jobOrchestrator,
    ICloudDriveAccountContextAccessor accountContextAccessor,
    IOptions<CloudDriveAppConfig> configOptions)
    : ICommandHandler<DeleteFromRecycleBinCommand, CloudDriveOperationResultDto>
{
    public async Task<Result<CloudDriveOperationResultDto>> Handle(
        DeleteFromRecycleBinCommand request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        string cloudAccountUid = accountContextAccessor.CloudAccountUid;

        if (CloudDriveFileHandlerSupport.ShouldRunBatchAsync(request.Input.RecycleBinItemIds.Count))
        {
            Guid jobId = await jobOrchestrator.EnqueueBatchOperationAsync(
                request.UserId,
                cloudAccountUid,
                CloudDriveJobType.BatchRecycleBinDelete,
                request.Input,
                cancellationToken);

            return Result.Success(CloudDriveFileHandlerSupport.CreateAcceptedResult(jobId, "回收站彻底删除任务已受理。"));
        }

        return Result.Failure<CloudDriveOperationResultDto>(
            CloudDriveFileHandlerSupport.ApiNotCollected("回收站彻底删除"));
    }
}
