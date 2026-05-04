using System.Text.Json;

using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.CloudDrive.Files;
using Application.Queries;

using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Share;

internal sealed class GetMySharesQueryHandler(
    ICloudDriveOpenApiGateway openApiGateway,
    IOptions<CloudDriveAppConfig> configOptions)
    : IQueryHandler<GetMySharesQuery, PagedResultDto<CloudDriveShareItemDto>>
{
    public async Task<Result<PagedResultDto<CloudDriveShareItemDto>>> Handle(
        GetMySharesQuery request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        if (string.IsNullOrWhiteSpace(request.Input.CloudAccountUid))
        {
            return Result.Failure<PagedResultDto<CloudDriveShareItemDto>>(
                CloudDriveFileHandlerSupport.CloudAccountUidRequired);
        }

        JsonElement response = await openApiGateway.SendWebApiRequestAsync<JsonElement>(
            request.UserId,
            request.Input.CloudAccountUid,
            HttpMethod.Get,
            "share/slist",
            CloudDriveShareSupport.CreateListContext(request.Input),
            cancellationToken);

        try
        {
            return Result.Success(CloudDriveShareSupport.MapList(response, request.Input, item => item));
        }
        catch (InvalidOperationException)
        {
            return Result.Failure<PagedResultDto<CloudDriveShareItemDto>>(
                CloudDriveShareSupport.ShareSampleUnavailable);
        }
    }
}
