using System.Text.Json;

using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.CloudDrive.Files;
using Application.Commands;

using Domain.Exceptions;
using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Offline;

internal sealed class ClearOfflineTasksCommandHandler(
    ICloudDriveOpenApiGateway openApiGateway,
    IOptions<CloudDriveAppConfig> configOptions)
    : ICommandHandler<ClearOfflineTasksCommand, CloudDriveOperationResultDto>
{
    public async Task<Result<CloudDriveOperationResultDto>> Handle(
        ClearOfflineTasksCommand request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        if (string.IsNullOrWhiteSpace(request.Input.CloudAccountUid))
        {
            return Result.Failure<CloudDriveOperationResultDto>(
                CloudDriveFileHandlerSupport.CloudAccountUidRequired);
        }

        try
        {
            await openApiGateway.SendOfflineApiRequestAsync<JsonElement>(
                request.UserId,
                request.Input.CloudAccountUid,
                HttpMethod.Post,
                "task_clear",
                CloudDriveOfflineSupport.CreateClearTasksContext(request.Input),
                cancellationToken);
        }
        catch (CloudDriveConfigurationException ex) when (CloudDriveOfflineSupport.IsOfflineSignatureUnavailable(ex))
        {
            return Result.Failure<CloudDriveOperationResultDto>(CloudDriveOfflineSupport.OfflineSignatureUnavailable);
        }

        return Result.Success(CloudDriveOfflineSupport.CreateSyncResult("云下载任务清理完成。"));
    }
}
