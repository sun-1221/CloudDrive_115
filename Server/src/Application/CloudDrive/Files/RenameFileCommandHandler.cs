using System.Text.Json;

using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.Commands;

using Domain.Results;
using Domain.Validators;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Files;

internal sealed class RenameFileCommandHandler(
    CloudDriveFileManager fileManager,
    ICloudDriveOpenApiGateway openApiGateway,
    IOptions<CloudDriveAppConfig> configOptions)
    : ICommandHandler<RenameFileCommand, CloudDriveFileDetailDto>
{
    public async Task<Result<CloudDriveFileDetailDto>> Handle(
        RenameFileCommand request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        if (string.IsNullOrWhiteSpace(request.Input.CloudAccountUid))
        {
            return Result.Failure<CloudDriveFileDetailDto>(
                CloudDriveFileHandlerSupport.CloudAccountUidRequired);
        }

        CloudDriveFileNameValidator.ValidateRenameFileName(request.Input.NewFileName);

        CloudDriveFileDetailDto result = await fileManager.ExecuteWithCacheInvalidationAsync(
            request.UserId,
            request.Input.CloudAccountUid,
            ["0"],
            async () =>
            {
                JsonElement response = await openApiGateway.SendWebApiRequestAsync<JsonElement>(
                    request.UserId,
                    request.Input.CloudAccountUid,
                    HttpMethod.Post,
                    "files/edit",
                    CloudDriveFileHandlerSupport.CreateRenameContext(request.Input),
                    cancellationToken);

                return CloudDriveFileHandlerSupport.CreateRenamedFileDto(response, request.Input);
            },
            cancellationToken);

        return Result.Success(result);
    }
}
