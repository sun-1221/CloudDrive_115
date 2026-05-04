using System.Text.Json;

using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.Commands;

using Domain.Enums;
using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Files;

internal sealed class CopyFilesCommandHandler(
    CloudDriveFileManager fileManager,
    ICloudDriveOpenApiGateway openApiGateway,
    ICloudDriveJobOrchestrator jobOrchestrator,
    IOptions<CloudDriveAppConfig> configOptions)
    : ICommandHandler<CopyFilesCommand, CloudDriveOperationResultDto>
{
    public async Task<Result<CloudDriveOperationResultDto>> Handle(
        CopyFilesCommand request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        if (string.IsNullOrWhiteSpace(request.Input.CloudAccountUid))
        {
            return Result.Failure<CloudDriveOperationResultDto>(
                CloudDriveFileHandlerSupport.CloudAccountUidRequired);
        }

        if (CloudDriveFileHandlerSupport.ShouldRunBatchAsync(request.Input.FileIds.Count))
        {
            Guid jobId = await jobOrchestrator.EnqueueBatchOperationAsync(
                request.UserId,
                request.Input.CloudAccountUid,
                CloudDriveJobType.BatchCopy,
                request.Input,
                cancellationToken);

            return Result.Success(CloudDriveFileHandlerSupport.CreateAcceptedResult(jobId, "复制任务已受理。"));
        }

        CloudDriveOperationResultDto result = await fileManager.ExecuteWithCacheInvalidationAsync(
            request.UserId,
            request.Input.CloudAccountUid,
            [request.Input.TargetDirectoryId],
            async () =>
            {
                await openApiGateway.SendWebApiRequestAsync<JsonElement>(
                    request.UserId,
                    request.Input.CloudAccountUid,
                    HttpMethod.Post,
                    "files/copy",
                    CloudDriveFileHandlerSupport.CreateCopyContext(request.Input),
                    cancellationToken);

                return CloudDriveFileHandlerSupport.CreateSyncResult("复制完成。");
            },
            cancellationToken);

        return Result.Success(result);
    }
}
