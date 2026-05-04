using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.Queries;

using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Files;

internal sealed class GetDownloadUrlQueryHandler(
    ICloudDriveOpenApiGateway openApiGateway,
    IOptions<CloudDriveAppConfig> configOptions)
    : IQueryHandler<GetDownloadUrlQuery, CloudDriveDownloadDto>
{
    public async Task<Result<CloudDriveDownloadDto>> Handle(
        GetDownloadUrlQuery request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        if (string.IsNullOrWhiteSpace(request.Input.CloudAccountUid))
        {
            return Result.Failure<CloudDriveDownloadDto>(
                CloudDriveFileHandlerSupport.CloudAccountUidRequired);
        }

        CloudDriveDownloadDto result = await openApiGateway.SendWebApiRequestAsync<CloudDriveDownloadDto>(
            request.UserId,
            request.Input.CloudAccountUid,
            HttpMethod.Get,
            "files/download",
            CloudDriveFileHandlerSupport.CreateDownloadContext(request.Input),
            cancellationToken);

        result.IsTemporary = true;
        if (string.IsNullOrWhiteSpace(result.DownloadUrl))
        {
            return Result.Failure<CloudDriveDownloadDto>(
                CloudDriveFileHandlerSupport.InvalidInput(
                    "cloud_drive_download_url_unavailable",
                    "下载地址不可用，请稍后重试。"));
        }

        return Result.Success(result);
    }
}
