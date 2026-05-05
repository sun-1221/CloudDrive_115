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
    ICloudDriveAccountContextAccessor accountContextAccessor,
    IOptions<CloudDriveAppConfig> configOptions)
    : IQueryHandler<GetOfflineQuotaQuery, CloudDriveOfflineQuotaDto>
{
    public async Task<Result<CloudDriveOfflineQuotaDto>> Handle(
        GetOfflineQuotaQuery request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        string cloudAccountUid = accountContextAccessor.CloudAccountUid;

        JsonElement quota = await openApiGateway.SendOfflineApiRequestAsync<JsonElement>(
            request.UserId,
            cloudAccountUid,
            HttpMethod.Get,
            "get_quota_package_info",
            CloudDriveOfflineSupport.CreateQuotaContext(),
            cancellationToken);

        return Result.Success(CloudDriveOfflineSupport.MapQuota(quota));
    }
}
