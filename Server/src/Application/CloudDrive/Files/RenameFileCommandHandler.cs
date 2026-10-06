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
    ICloudDriveWebApiGateway openApiGateway,
    ICloudDriveAccountContextAccessor accountContextAccessor,
    ICloudDriveMetadataCache metadataCache,
    IOptions<CloudDriveAppConfig> configOptions)
    : ICommandHandler<RenameFileCommand, CloudDriveFileDetailDto>
{
    public async Task<Result<CloudDriveFileDetailDto>> Handle(
        RenameFileCommand request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);
        CloudDriveFileNameValidator.ValidateRenameFileName(request.Input.NewFileName);

        JsonElement response = await openApiGateway.SendWebApiRequestAsync<JsonElement>(
            request.UserId,
            accountContextAccessor.CloudAccountUid,
            HttpMethod.Post,
            "files/batch_rename",
            CloudDriveFileHandlerSupport.CreateRenameContext(
                request.Input.FileId,
                request.Input.NewFileName),
            cancellationToken);
        CloudDriveFileHandlerSupport.EnsureSuccessEnvelope(
            response,
            "重命名文件",
            "errno",
            JsonValueKind.Number);
        await metadataCache.InvalidateFileAsync(
            request.UserId,
            accountContextAccessor.CloudAccountUid,
            request.Input.FileId,
            cancellationToken);

        return Result.Success(new CloudDriveFileDetailDto
        {
            FileId = request.Input.FileId,
            Name = request.Input.NewFileName,
        });
    }
}
