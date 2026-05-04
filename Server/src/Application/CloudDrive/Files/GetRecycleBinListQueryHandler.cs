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
    : IQueryHandler<GetRecycleBinListQuery, PagedResultDto<CloudDriveRecycleBinItemDto>>
{
    public async Task<Result<PagedResultDto<CloudDriveRecycleBinItemDto>>> Handle(
        GetRecycleBinListQuery request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        if (string.IsNullOrWhiteSpace(request.Input.CloudAccountUid))
        {
            return Result.Failure<PagedResultDto<CloudDriveRecycleBinItemDto>>(
                CloudDriveFileHandlerSupport.CloudAccountUidRequired);
        }

        PagedResultDto<CloudDriveRecycleBinItemDto> result =
            await openApiGateway.SendWebApiRequestAsync<PagedResultDto<CloudDriveRecycleBinItemDto>>(
                request.UserId,
                request.Input.CloudAccountUid,
                HttpMethod.Get,
                "rb",
                CloudDriveFileHandlerSupport.CreateRecycleBinListContext(request.Input),
                cancellationToken);

        return Result.Success(result);
    }
}
