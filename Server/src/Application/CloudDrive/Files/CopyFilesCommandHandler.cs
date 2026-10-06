using System.Text.Json;

using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.Commands;

using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Files;

internal sealed class CopyFilesCommandHandler(
    ICloudDriveWebApiGateway openApiGateway,
    ICloudDriveAccountContextAccessor accountContextAccessor,
    ICloudDriveMetadataCache metadataCache,
    IOptions<CloudDriveAppConfig> configOptions)
    : ICommandHandler<CopyFilesCommand, CloudDriveOperationResultDto>
{
    public async Task<Result<CloudDriveOperationResultDto>> Handle(
        CopyFilesCommand request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        if (request.Input.FileIds.Count != 1)
        {
            return Result.Failure<CloudDriveOperationResultDto>(
                CloudDriveFileHandlerSupport.InvalidInput(
                    "cloud_drive_copy_single_file_required",
                    "文件复制当前只支持单个 fileId。"));
        }

        JsonElement response = await openApiGateway.SendWebApiRequestAsync<JsonElement>(
            request.UserId,
            accountContextAccessor.CloudAccountUid,
            HttpMethod.Post,
            "files/copy",
            CloudDriveFileHandlerSupport.CreateCopyContext(
                request.Input.FileIds[0],
                request.Input.TargetDirectoryId),
            cancellationToken);
        CloudDriveFileHandlerSupport.EnsureSuccessEnvelope(
            response,
            "复制文件",
            "errno",
            JsonValueKind.String);
        await metadataCache.InvalidateDirectoryAsync(
            request.UserId,
            accountContextAccessor.CloudAccountUid,
            request.Input.TargetDirectoryId,
            cancellationToken);

        return Result.Success(CloudDriveFileHandlerSupport.CreateSyncResult("文件复制完成。"));
    }
}
