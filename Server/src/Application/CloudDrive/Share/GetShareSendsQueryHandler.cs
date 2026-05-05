using System.Text.Json;

using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.CloudDrive.Files;
using Application.Queries;

using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Share;

internal sealed class GetShareSendsQueryHandler(
    ICloudDriveOpenApiGateway openApiGateway,
    ICloudDriveAccountContextAccessor accountContextAccessor,
    IOptions<CloudDriveAppConfig> configOptions)
    : IQueryHandler<GetShareSendsQuery, CloudDriveShareListDto>
{
    public async Task<Result<CloudDriveShareListDto>> Handle(
        GetShareSendsQuery request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        string cloudAccountUid = accountContextAccessor.CloudAccountUid;

        JsonElement response = await openApiGateway.SendWebApiRequestAsync<JsonElement>(
            request.UserId,
            cloudAccountUid,
            HttpMethod.Get,
            "share/send",
            CloudDriveShareSupport.CreateListContext(request.Input),
            cancellationToken);

        return Result.Success(CloudDriveShareSupport.MapList(response));
    }
}
