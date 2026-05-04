using System.Text.Json;

using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.CloudDrive.Files;
using Application.Queries;

using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Offline;

internal sealed class GetOfflineTaskListQueryHandler(
    ICloudDriveOpenApiGateway openApiGateway,
    IOptions<CloudDriveAppConfig> configOptions)
    : IQueryHandler<GetOfflineTaskListQuery, CloudDriveOfflineTaskListDto>
{
    public async Task<Result<CloudDriveOfflineTaskListDto>> Handle(
        GetOfflineTaskListQuery request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        if (string.IsNullOrWhiteSpace(request.Input.CloudAccountUid))
        {
            return Result.Failure<CloudDriveOfflineTaskListDto>(
                CloudDriveFileHandlerSupport.CloudAccountUidRequired);
        }

        JsonElement response = await openApiGateway.SendOfflineApiRequestAsync<JsonElement>(
            request.UserId,
            request.Input.CloudAccountUid,
            HttpMethod.Post,
            "task_lists",
            CloudDriveOfflineSupport.CreateTaskListContext(request.Input),
            cancellationToken);

        return Result.Success(CloudDriveOfflineSupport.MapTaskList(response, request.Input));
    }
}
