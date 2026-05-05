using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.Commands;

using Domain.Results;
using Domain.Validators;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Files;

internal sealed class CreateFolderCommandHandler(
    IOptions<CloudDriveAppConfig> configOptions)
    : ICommandHandler<CreateFolderCommand, CloudDriveFolderDto>
{
    public Task<Result<CloudDriveFolderDto>> Handle(
        CreateFolderCommand request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        CloudDriveFileNameValidator.ValidateNewFolderName(request.Input.FolderName);

        return Task.FromResult(Result.Failure<CloudDriveFolderDto>(
            CloudDriveFileHandlerSupport.ApiNotCollected("新建文件夹")));
    }
}
