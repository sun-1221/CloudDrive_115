using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.CloudDrive.Files;
using Application.Commands;

using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Offline;

internal sealed class AddOfflineTaskCommandHandler(
    IOptions<CloudDriveAppConfig> configOptions)
    : ICommandHandler<AddOfflineTaskCommand, CloudDriveOperationResultDto>
{
    public Task<Result<CloudDriveOperationResultDto>> Handle(
        AddOfflineTaskCommand request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        if (string.IsNullOrWhiteSpace(request.Input.CloudAccountUid))
        {
            return Task.FromResult(Result.Failure<CloudDriveOperationResultDto>(
                CloudDriveFileHandlerSupport.CloudAccountUidRequired));
        }

        return Task.FromResult(Result.Failure<CloudDriveOperationResultDto>(
            CloudDriveFileHandlerSupport.ApiNotCollected("云下载创建任务")));
    }
}
