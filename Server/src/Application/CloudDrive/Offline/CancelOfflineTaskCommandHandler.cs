using Application.CloudDrive.Dtos;
using Application.CloudDrive.Files;
using Application.Commands;

using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Offline;

internal sealed class CancelOfflineTaskCommandHandler(IOptions<CloudDriveAppConfig> configOptions)
    : ICommandHandler<CancelOfflineTaskCommand, CloudDriveOperationResultDto>
{
    public Task<Result<CloudDriveOperationResultDto>> Handle(
        CancelOfflineTaskCommand request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Failure<CloudDriveOperationResultDto>(
            CloudDriveFileHandlerSupport.ApiNotCollected("云下载取消任务")));
    }
}
