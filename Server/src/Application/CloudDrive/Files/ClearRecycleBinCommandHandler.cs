using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.Commands;

using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Files;

internal sealed class ClearRecycleBinCommandHandler(
    ICloudDriveJobOrchestrator jobOrchestrator,
    IOptions<CloudDriveAppConfig> configOptions)
    : ICommandHandler<ClearRecycleBinCommand, CloudDriveOperationResultDto>
{
    public async Task<Result<CloudDriveOperationResultDto>> Handle(
        ClearRecycleBinCommand request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        if (string.IsNullOrWhiteSpace(request.Input.CloudAccountUid))
        {
            return Result.Failure<CloudDriveOperationResultDto>(
                CloudDriveFileHandlerSupport.CloudAccountUidRequired);
        }

        Guid jobId = await jobOrchestrator.EnqueueClearRecycleBinAsync(
            request.UserId,
            request.Input.CloudAccountUid,
            cancellationToken);

        return Result.Success(CloudDriveFileHandlerSupport.CreateAcceptedResult(jobId, "清空回收站任务已受理。"));
    }
}
