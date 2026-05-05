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
    ICloudDriveAccountContextAccessor accountContextAccessor,
    IOptions<CloudDriveAppConfig> configOptions)
    : IQueryHandler<SearchFilesQuery, CloudDriveSearchListDto>
{
    public async Task<Result<CloudDriveSearchListDto>> Handle(
        SearchFilesQuery request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        string cloudAccountUid = accountContextAccessor.CloudAccountUid;

        if (string.IsNullOrWhiteSpace(request.Input.Keyword))
        {
            return Result.Failure<CloudDriveSearchListDto>(
                CloudDriveFileHandlerSupport.InvalidInput(
                    "cloud_drive_search_keyword_required",
                    "搜索关键字不能为空。"));
        }

        CloudDriveApiContext context = CloudDriveFileHandlerSupport.CreateSearchContext(request.Input);
        using IDisposable permit = await rateLimiter.AcquireAsync(
            request.UserId,
            cloudAccountUid,
            context.Category,
            cancellationToken);

        CloudDriveSearchListDto result =
            await openApiGateway.SendWebApiRequestAsync<CloudDriveSearchListDto>(
                request.UserId,
                cloudAccountUid,
                HttpMethod.Get,
                "files/search",
                context,
                cancellationToken);

        return Result.Success(result);
    }
}
