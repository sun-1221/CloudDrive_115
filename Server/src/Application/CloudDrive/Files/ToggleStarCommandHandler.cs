using System.Text.Json;

using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.Commands;

using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Files;

internal sealed class ToggleStarCommandHandler(
    CloudDriveFileManager fileManager,
    ICloudDriveOpenApiGateway openApiGateway,
    IOptions<CloudDriveAppConfig> configOptions)
    : ICommandHandler<ToggleStarCommand, CloudDriveOperationResultDto>
{
    public async Task<Result<CloudDriveOperationResultDto>> Handle(
        ToggleStarCommand request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        if (string.IsNullOrWhiteSpace(request.Input.CloudAccountUid))
        {
            return Result.Failure<CloudDriveOperationResultDto>(
                CloudDriveFileHandlerSupport.CloudAccountUidRequired);
        }

        CloudDriveOperationResultDto result = await fileManager.ExecuteWithCacheInvalidationAsync(
            request.UserId,
            request.Input.CloudAccountUid,
            ["0"],
            async () =>
            {
                await openApiGateway.SendWebApiRequestAsync<JsonElement>(
                    request.UserId,
                    request.Input.CloudAccountUid,
                    HttpMethod.Post,
                    "files/star",
                    CloudDriveFileHandlerSupport.CreateToggleStarContext(request.Input),
                    cancellationToken);

                return CloudDriveFileHandlerSupport.CreateSyncResult(
                    request.Input.Star ? "已添加星标。" : "已取消星标。");
            },
            cancellationToken);

        return Result.Success(result);
    }
}
