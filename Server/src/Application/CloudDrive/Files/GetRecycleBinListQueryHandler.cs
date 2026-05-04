using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.Queries;

using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Files;

internal sealed class GetRecycleBinListQueryHandler(
    ICloudDriveOpenApiGateway openApiGateway,
    IOptions<CloudDriveAppConfig> configOptions)
    : IQueryHandler<GetRecycleBinListQuery, CloudDriveRecycleListDto>
{
    public async Task<Result<CloudDriveRecycleListDto>> Handle(
        GetRecycleBinListQuery request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        if (string.IsNullOrWhiteSpace(request.Input.CloudAccountUid))
        {
            return Result.Failure<CloudDriveRecycleListDto>(
                CloudDriveFileHandlerSupport.CloudAccountUidRequired);
        }

        CloudDriveRecycleListDto result =
            await openApiGateway.SendWebApiRequestAsync<CloudDriveRecycleListDto>(
                request.UserId,
                request.Input.CloudAccountUid,
                HttpMethod.Get,
                "rb",
                CloudDriveFileHandlerSupport.CreateRecycleBinListContext(request.Input),
                cancellationToken);

        return Result.Success(result);
    }
}
