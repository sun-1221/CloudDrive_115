using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.Commands;

using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Files;

internal sealed class ClearRecycleBinCommandHandler(
    ICloudDriveJobOrchestrator jobOrchestrator,
    ICloudDriveAccountContextAccessor accountContextAccessor,
    IOptions<CloudDriveAppConfig> configOptions)
    : ICommandHandler<ClearRecycleBinCommand, CloudDriveOperationResultDto>
{
    public async Task<Result<CloudDriveOperationResultDto>> Handle(
        ClearRecycleBinCommand request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        string cloudAccountUid = accountContextAccessor.CloudAccountUid;

        Guid jobId = await jobOrchestrator.EnqueueClearRecycleBinAsync(
            request.UserId,
            cloudAccountUid,
            cancellationToken);

        return Result.Success(CloudDriveFileHandlerSupport.CreateAcceptedResult(jobId, "清空回收站任务已受理。"));
    }
}
