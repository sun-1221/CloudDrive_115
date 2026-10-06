using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.Queries;

using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Files;

internal sealed class GetRecycleBinListQueryHandler(
    ICloudDriveWebApiGateway openApiGateway,
    ICloudDriveAccountContextAccessor accountContextAccessor,
    IOptions<CloudDriveAppConfig> configOptions)
    : IQueryHandler<GetRecycleBinListQuery, CloudDriveRecycleListDto>
{
    public async Task<Result<CloudDriveRecycleListDto>> Handle(
        GetRecycleBinListQuery request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        string cloudAccountUid = accountContextAccessor.CloudAccountUid;

        CloudDriveRecycleListDto result =
            await openApiGateway.SendWebApiRequestAsync<CloudDriveRecycleListDto>(
                request.UserId,
                cloudAccountUid,
                HttpMethod.Get,
                "rb",
                CloudDriveFileHandlerSupport.CreateRecycleBinListContext(request.Input),
                cancellationToken);

        return Result.Success(result);
    }
}
