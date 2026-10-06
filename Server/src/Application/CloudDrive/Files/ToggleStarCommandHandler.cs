using System.Text.Json;

using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.Commands;

using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Files;

internal sealed class ToggleStarCommandHandler(
    ICloudDriveWebApiGateway openApiGateway,
    ICloudDriveAccountContextAccessor accountContextAccessor,
    ICloudDriveMetadataCache metadataCache,
    IOptions<CloudDriveAppConfig> configOptions)
    : ICommandHandler<ToggleStarCommand, CloudDriveOperationResultDto>
{
    public async Task<Result<CloudDriveOperationResultDto>> Handle(
        ToggleStarCommand request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        JsonElement response = await openApiGateway.SendWebApiRequestAsync<JsonElement>(
            request.UserId,
            accountContextAccessor.CloudAccountUid,
            HttpMethod.Post,
            "files/star",
            CloudDriveFileHandlerSupport.CreateToggleStarContext(request.Input),
            cancellationToken);
        CloudDriveFileHandlerSupport.EnsureSuccessEnvelope(response, "更新星标");
        await metadataCache.InvalidateFileAsync(
            request.UserId,
            accountContextAccessor.CloudAccountUid,
            request.Input.FileId,
            cancellationToken);

        return Result.Success(CloudDriveFileHandlerSupport.CreateSyncResult("星标状态已更新。"));
    }
}
