using System.Text.Json;

using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.Commands;

using Domain.Results;
using Domain.Validators;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Files;

internal sealed class CreateFolderCommandHandler(
    CloudDriveFileManager fileManager,
    ICloudDriveOpenApiGateway openApiGateway,
    IOptions<CloudDriveAppConfig> configOptions)
    : ICommandHandler<CreateFolderCommand, CloudDriveFolderDto>
{
    public async Task<Result<CloudDriveFolderDto>> Handle(
        CreateFolderCommand request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        if (string.IsNullOrWhiteSpace(request.Input.CloudAccountUid))
        {
            return Result.Failure<CloudDriveFolderDto>(
                CloudDriveFileHandlerSupport.CloudAccountUidRequired);
        }

        CloudDriveFileNameValidator.ValidateNewFolderName(request.Input.FolderName);

        CloudDriveFolderDto result = await fileManager.ExecuteWithCacheInvalidationAsync(
            request.UserId,
            request.Input.CloudAccountUid,
            [request.Input.ParentId],
            async () =>
            {
                JsonElement response = await openApiGateway.SendWebApiRequestAsync<JsonElement>(
                    request.UserId,
                    request.Input.CloudAccountUid,
                    HttpMethod.Post,
                    "files/add",
                    CloudDriveFileHandlerSupport.CreateCreateFolderContext(request.Input),
                    cancellationToken);

                return CloudDriveFileHandlerSupport.CreateFolderDto(response, request.Input);
            },
            cancellationToken);

        return Result.Success(result);
    }
}
