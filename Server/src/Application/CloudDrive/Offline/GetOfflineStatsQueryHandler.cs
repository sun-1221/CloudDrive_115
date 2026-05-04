using System.Text.Json;

using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.CloudDrive.Files;
using Application.Queries;

using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Offline;

internal sealed class GetOfflineStatsQueryHandler(
    ICloudDriveOpenApiGateway openApiGateway,
    IOptions<CloudDriveAppConfig> configOptions)
    : IQueryHandler<GetOfflineStatsQuery, CloudDriveOfflineStatsDto>
{
    public async Task<Result<CloudDriveOfflineStatsDto>> Handle(
        GetOfflineStatsQuery request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        if (string.IsNullOrWhiteSpace(request.Input.CloudAccountUid))
        {
            return Result.Failure<CloudDriveOfflineStatsDto>(
                CloudDriveFileHandlerSupport.CloudAccountUidRequired);
        }

        JsonElement stats = await openApiGateway.SendOfflineApiRequestAsync<JsonElement>(
            request.UserId,
            request.Input.CloudAccountUid,
            HttpMethod.Post,
            "task_count",
            CloudDriveOfflineSupport.CreateStatsContext(),
            cancellationToken);

        return Result.Success(CloudDriveOfflineSupport.MapStats(stats));
    }
}
