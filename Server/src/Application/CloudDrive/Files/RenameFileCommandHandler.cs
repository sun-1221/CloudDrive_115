using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.Commands;

using Domain.Results;
using Domain.Validators;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Files;

internal sealed class RenameFileCommandHandler(
    IOptions<CloudDriveAppConfig> configOptions)
    : ICommandHandler<RenameFileCommand, CloudDriveFileDetailDto>
{
    public Task<Result<CloudDriveFileDetailDto>> Handle(
        RenameFileCommand request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        if (string.IsNullOrWhiteSpace(request.Input.CloudAccountUid))
        {
            return Task.FromResult(Result.Failure<CloudDriveFileDetailDto>(
                CloudDriveFileHandlerSupport.CloudAccountUidRequired));
        }

        CloudDriveFileNameValidator.ValidateRenameFileName(request.Input.NewFileName);

        return Task.FromResult(Result.Failure<CloudDriveFileDetailDto>(
            CloudDriveFileHandlerSupport.ApiNotCollected("文件重命名")));
    }
}
