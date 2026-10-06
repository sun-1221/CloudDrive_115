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
    ICloudDriveWebApiGateway openApiGateway,
    ICloudDriveAccountContextAccessor accountContextAccessor,
    ICloudDriveMetadataCache metadataCache,
    IOptions<CloudDriveAppConfig> configOptions)
    : ICommandHandler<CreateFolderCommand, CloudDriveFolderDto>
{
    public async Task<Result<CloudDriveFolderDto>> Handle(
        CreateFolderCommand request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);
        CloudDriveFileNameValidator.ValidateNewFolderName(request.Input.FolderName);

        JsonElement response = await openApiGateway.SendWebApiRequestAsync<JsonElement>(
            request.UserId,
            accountContextAccessor.CloudAccountUid,
            HttpMethod.Post,
            "files/add",
            CloudDriveFileHandlerSupport.CreateCreateFolderContext(request.Input),
            cancellationToken);
        CloudDriveFileHandlerSupport.EnsureSuccessEnvelope(
            response,
            "新建文件夹",
            "errno",
            JsonValueKind.String);
        string folderId = CloudDriveFileHandlerSupport.RequireString(response, "file_id", "新建文件夹");
        string folderName = CloudDriveFileHandlerSupport.RequireString(response, "file_name", "新建文件夹");
        await metadataCache.InvalidateDirectoryAsync(
            request.UserId,
            accountContextAccessor.CloudAccountUid,
            request.Input.ParentId,
            cancellationToken);

        return Result.Success(new CloudDriveFolderDto
        {
            FolderId = folderId,
            Name = folderName,
            ParentId = request.Input.ParentId,
        });
    }
}
