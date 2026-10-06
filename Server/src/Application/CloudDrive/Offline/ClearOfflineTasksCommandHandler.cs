using Application.CloudDrive.Dtos;
using Application.CloudDrive.Files;
using Application.Commands;

using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Offline;

internal sealed class ClearOfflineTasksCommandHandler(IOptions<CloudDriveAppConfig> configOptions)
    : ICommandHandler<ClearOfflineTasksCommand, CloudDriveOperationResultDto>
{
    public Task<Result<CloudDriveOperationResultDto>> Handle(
        ClearOfflineTasksCommand request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Failure<CloudDriveOperationResultDto>(
            CloudDriveFileHandlerSupport.ApiNotCollected("云下载清空任务")));
    }
}
