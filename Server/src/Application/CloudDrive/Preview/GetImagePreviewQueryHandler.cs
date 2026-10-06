using System.Text.Json;

using Application.Abstractions.CloudDrive;

using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.CloudDrive.Files;
using Application.CloudDrive.Mappings;
using Application.Queries;

using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Preview;

internal sealed class GetImagePreviewQueryHandler(
    ICloudDriveWebApiGateway openApiGateway,
    ICloudDriveAccountContextAccessor accountContextAccessor,
    IOptions<CloudDriveAppConfig> configOptions)
    : IQueryHandler<GetImagePreviewQuery, CloudDriveImagePreviewDto>
{
    public async Task<Result<CloudDriveImagePreviewDto>> Handle(
        GetImagePreviewQuery request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        JsonElement response = await openApiGateway.SendWebApiRequestAsync<JsonElement>(
            request.UserId,
            accountContextAccessor.CloudAccountUid,
            HttpMethod.Get,
            "files/download",
            CloudDrivePreviewSupport.CreateDownloadContext(request.Input),
            cancellationToken);

        CloudDriveDownloadDto download = CloudDrivePreviewSupport.MapDownload(response);
        if (string.IsNullOrWhiteSpace(download.FileName))
        {
            return Result.Failure<CloudDriveImagePreviewDto>(
                CloudDriveFileHandlerSupport.ApiNotCollected("下载文件名"));
        }

        string extension = CloudDrivePreviewSupport.ResolveExtension(new CloudDriveFileDto
        {
            Name = download.FileName,
        });
        if (!CloudDrivePreviewSupport.IsImage(extension))
        {
            return Result.Failure<CloudDriveImagePreviewDto>(CloudDrivePreviewSupport.UnsupportedImageType);
        }

        if (string.IsNullOrWhiteSpace(download.DownloadUrl))
        {
            return Result.Failure<CloudDriveImagePreviewDto>(
                CloudDriveFileHandlerSupport.ApiNotCollected("图片下载地址"));
        }

        return Result.Success(download.ToDto(extension));
    }
}
