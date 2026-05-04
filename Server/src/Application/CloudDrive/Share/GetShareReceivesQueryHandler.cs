using System.Text.Json;

using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.CloudDrive.Files;
using Application.Queries;

using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Share;

internal sealed class GetShareReceivesQueryHandler(
    ICloudDriveOpenApiGateway openApiGateway,
    IOptions<CloudDriveAppConfig> configOptions)
    : IQueryHandler<GetShareReceivesQuery, PagedResultDto<CloudDriveShareReceiveItemDto>>
{
    public async Task<Result<PagedResultDto<CloudDriveShareReceiveItemDto>>> Handle(
        GetShareReceivesQuery request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        if (string.IsNullOrWhiteSpace(request.Input.CloudAccountUid))
        {
            return Result.Failure<PagedResultDto<CloudDriveShareReceiveItemDto>>(
                CloudDriveFileHandlerSupport.CloudAccountUidRequired);
        }

        JsonElement response = await openApiGateway.SendWebApiRequestAsync<JsonElement>(
            request.UserId,
            request.Input.CloudAccountUid,
            HttpMethod.Get,
            "share/receive",
            CloudDriveShareSupport.CreateListContext(request.Input),
            cancellationToken);

        try
        {
            return Result.Success(CloudDriveShareSupport.MapList(
                response,
                request.Input,
                CloudDriveShareSupport.ToReceiveItem));
        }
        catch (InvalidOperationException)
        {
            return Result.Failure<PagedResultDto<CloudDriveShareReceiveItemDto>>(
                CloudDriveShareSupport.ShareSampleUnavailable);
        }
    }
}
