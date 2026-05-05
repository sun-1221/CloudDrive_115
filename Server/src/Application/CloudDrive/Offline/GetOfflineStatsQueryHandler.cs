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
    ICloudDriveAccountContextAccessor accountContextAccessor,
    IOptions<CloudDriveAppConfig> configOptions)
    : IQueryHandler<GetOfflineStatsQuery, CloudDriveOfflineStatsDto>
{
    public async Task<Result<CloudDriveOfflineStatsDto>> Handle(
        GetOfflineStatsQuery request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        string cloudAccountUid = accountContextAccessor.CloudAccountUid;

        JsonElement stats = await openApiGateway.SendOfflineApiRequestAsync<JsonElement>(
            request.UserId,
            cloudAccountUid,
            HttpMethod.Post,
            "task_count",
            CloudDriveOfflineSupport.CreateStatsContext(),
            cancellationToken);

        return Result.Success(CloudDriveOfflineSupport.MapStats(stats));
    }
}
