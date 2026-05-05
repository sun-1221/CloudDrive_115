using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.Commands;

using Domain.Enums;
using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Files;

internal sealed class MoveFilesCommandHandler(
    ICloudDriveJobOrchestrator jobOrchestrator,
    ICloudDriveAccountContextAccessor accountContextAccessor,
    IOptions<CloudDriveAppConfig> configOptions)
    : ICommandHandler<MoveFilesCommand, CloudDriveOperationResultDto>
{
    public async Task<Result<CloudDriveOperationResultDto>> Handle(
        MoveFilesCommand request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        string cloudAccountUid = accountContextAccessor.CloudAccountUid;

        if (CloudDriveFileHandlerSupport.ShouldRunBatchAsync(request.Input.FileIds.Count))
        {
            Guid jobId = await jobOrchestrator.EnqueueBatchOperationAsync(
                request.UserId,
                cloudAccountUid,
                CloudDriveJobType.BatchMove,
                request.Input,
                cancellationToken);

            return Result.Success(CloudDriveFileHandlerSupport.CreateAcceptedResult(jobId, "移动任务已受理。"));
        }

        return Result.Failure<CloudDriveOperationResultDto>(
            CloudDriveFileHandlerSupport.ApiNotCollected("文件移动"));
    }
}
