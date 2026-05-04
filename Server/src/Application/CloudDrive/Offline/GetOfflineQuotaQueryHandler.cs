using System.Text.Json;

using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.CloudDrive.Files;
using Application.Queries;

using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Offline;

internal sealed class GetOfflineQuotaQueryHandler(
    ICloudDriveOpenApiGateway openApiGateway,
    IOptions<CloudDriveAppConfig> configOptions)
    : IQueryHandler<GetOfflineQuotaQuery, CloudDriveOfflineQuotaDto>
{
    public async Task<Result<CloudDriveOfflineQuotaDto>> Handle(
        GetOfflineQuotaQuery request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        if (string.IsNullOrWhiteSpace(request.Input.CloudAccountUid))
        {
            return Result.Failure<CloudDriveOfflineQuotaDto>(
                CloudDriveFileHandlerSupport.CloudAccountUidRequired);
        }

        JsonElement quota = await openApiGateway.SendOfflineApiRequestAsync<JsonElement>(
            request.UserId,
            request.Input.CloudAccountUid,
            HttpMethod.Get,
            "get_quota_package_info",
            CloudDriveOfflineSupport.CreateQuotaContext(),
            cancellationToken);

        JsonElement stats = await openApiGateway.SendOfflineApiRequestAsync<JsonElement>(
            request.UserId,
            request.Input.CloudAccountUid,
            HttpMethod.Post,
            "task_count",
            CloudDriveOfflineSupport.CreateStatsContext(),
            cancellationToken);

        return Result.Success(CloudDriveOfflineSupport.MapQuota(quota, stats));
    }
}
