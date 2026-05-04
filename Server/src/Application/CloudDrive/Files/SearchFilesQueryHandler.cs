using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.Queries;

using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Files;

internal sealed class SearchFilesQueryHandler(
    ICloudDriveOpenApiGateway openApiGateway,
    ICloudDriveRateLimiter rateLimiter,
    IOptions<CloudDriveAppConfig> configOptions)
    : IQueryHandler<SearchFilesQuery, PagedResultDto<CloudDriveSearchResultDto>>
{
    public async Task<Result<PagedResultDto<CloudDriveSearchResultDto>>> Handle(
        SearchFilesQuery request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        if (string.IsNullOrWhiteSpace(request.Input.CloudAccountUid))
        {
            return Result.Failure<PagedResultDto<CloudDriveSearchResultDto>>(
                CloudDriveFileHandlerSupport.CloudAccountUidRequired);
        }

        if (string.IsNullOrWhiteSpace(request.Input.Keyword))
        {
            return Result.Failure<PagedResultDto<CloudDriveSearchResultDto>>(
                CloudDriveFileHandlerSupport.InvalidInput(
                    "cloud_drive_search_keyword_required",
                    "搜索关键字不能为空。"));
        }

        CloudDriveApiContext context = CloudDriveFileHandlerSupport.CreateSearchContext(request.Input);
        using IDisposable permit = await rateLimiter.AcquireAsync(
            request.UserId,
            request.Input.CloudAccountUid,
            context.Category,
            cancellationToken);

        PagedResultDto<CloudDriveSearchResultDto> result =
            await openApiGateway.SendWebApiRequestAsync<PagedResultDto<CloudDriveSearchResultDto>>(
                request.UserId,
                request.Input.CloudAccountUid,
                HttpMethod.Get,
                "files/search",
                context,
                cancellationToken);

        return Result.Success(result);
    }
}
